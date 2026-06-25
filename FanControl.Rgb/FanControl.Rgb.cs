using System.Diagnostics.CodeAnalysis;
using System.Net.Sockets;
using FanControl.Plugins;
using OpenRGB.NET;
using FanControl.Rgb.Rules;
using FanControl.Rgb.Toolkit;

namespace FanControl.Rgb
{
  // Orchestrator: owns config + sensor bindings and the lifecycle of the
  // OpenRgbEngine, which holds all RGB/threading/state-machine concerns.
  public class OpenRgbPlugin : IPlugin
  {
    public string Name => "RGB";

    private readonly IPluginDialog _dialog;
    private readonly IPluginLogger _logger;
    private readonly string _configPath;
    private readonly Func<OpenRgbConfig, IOpenRgbBroker> _connect;
    private readonly Func<NzxtConfig, INzxtBridge> _connectNzxt;
    private readonly TimeProvider _time;
    private readonly Func<bool>? _suspended;

    private OpenRgbConfig _config = new();
    private IReadOnlyList<RuleBinding> _bindings = [];
    private OpenRgbEngine? _engine;
    private NzxtRenderer? _nzxtRenderer;

    public OpenRgbPlugin(IPluginDialog dialog, IPluginLogger logger)
    {
      _dialog = dialog;
      _logger = logger;
      _configPath = Path.Combine(PluginDirectory(), "RGBConfig.json");
      _connect = Connect;
      _connectNzxt = ConnectNzxt;
      _time = TimeProvider.System;
    }

    // Test seam: inject config and the engine's dependencies so Initialize can
    // run without touching the real config path or the OpenRGB server.
    internal OpenRgbPlugin(
        IPluginDialog dialog,
        IPluginLogger logger,
        OpenRgbConfig config,
        string? configPath = null,
        Func<OpenRgbConfig, IOpenRgbBroker>? connect = null,
        TimeProvider? time = null,
        Func<bool>? suspended = null,
        Func<NzxtConfig, INzxtBridge>? connectNzxt = null)
        : this(dialog, logger)
    {
      _config = config;
      if (configPath != null) _configPath = configPath;
      if (connect != null) _connect = connect;
      if (connectNzxt != null) _connectNzxt = connectNzxt;
      if (time != null) _time = time;
      _suspended = suspended;
    }

    internal bool EngineStarted => _engine != null;

    public void Initialize()
    {
      var loaded = ConfigLoader.LoadOrCreate(_configPath, _dialog, Log);
      if (loaded == null) return; // template generated, nothing to drive yet
      _config = loaded;

      _engine = new OpenRgbEngine(_config, _connect, Log, _time, _suspended, ShowFatal);
      _engine.SetBindings(_bindings);
      _engine.Start();

      // Command sink: NZXT RGB routed through the LiquidCtl bridge so it shares
      // the single serialized HID queue and never contends with the fan commands.
      if (_config.Nzxt is { Enabled: true } nzxt)
      {
        _nzxtRenderer = new NzxtRenderer(nzxt, _config.TransitionSpeed, _connectNzxt(nzxt), Log, _suspended);
        _nzxtRenderer.SetBindings(_bindings);
        _nzxtRenderer.Start();
      }
    }

    // NZXT bridge factory handed to the renderer; the only place that opens the
    // real pipe, so the renderer stays unit-testable behind INzxtBridge.
    [ExcludeFromCodeCoverage]
    private INzxtBridge ConnectNzxt(NzxtConfig nzxt) => new NzxtBridge(nzxt.PipeName, Log);

    // Connection factory handed to the engine; the only place that touches the
    // real OpenRGB SDK, so the engine stays unit-testable behind IOpenRgbBroker.
    // Excluded from coverage: it opens a real socket, untestable without a server.
    private const int ProbeTimeoutMs = 500;

    [ExcludeFromCodeCoverage]
    private static IOpenRgbBroker Connect(OpenRgbConfig config)
    {
      // Pre-flight reachability probe. OpenRGB.NET's SocketExtensions.Connect starts
      // socket.ConnectAsync, then Close()s it on timeout when the server is absent;
      // the aborted connect Task faults with SocketException(995) and is never awaited,
      // so the GC finalizer raises it as a TaskScheduler.UnobservedTaskException that
      // FanControl logs. That Task is a library-local var we cannot observe, so the
      // only fix is to never start it: connect through the SDK only once the port
      // actually accepts. APM (BeginConnect) avoids creating an observable Task here.
      EnsureReachable(config.ServerIp, config.ServerPort);

      var client = new OpenRgbClient(name: "FanControl", ip: config.ServerIp, port: config.ServerPort, autoConnect: false);
      try
      {
        client.Connect();
      }
      catch
      {
        // Connect failed: dispose the client so its background socket thread
        // is not leaked. The engine retries, so a leak here would accumulate
        // threads across reconnect attempts and starve the host.
        client.Dispose();
        throw;
      }
      return new OpenRgbBroker(client);
    }

    // Fast TCP reachability check. The connect attempt is fully awaited so it can
    // never fault unobserved: on timeout the token cancels ConnectAsync, the OCE
    // propagates *through* the awaited ValueTask, and we catch it. Abandoning a
    // connect instead (Task.WaitAny+Close, or BeginConnect+Close — which .NET 8
    // implements over ConnectAsync().AsTask()) is exactly what leaks the
    // SocketException(995) the host logs; we must observe it, not orphan it.
    // Throws on timeout/refusal so the engine applies its normal backoff.
    [ExcludeFromCodeCoverage]
    private static void EnsureReachable(string ip, int port)
    {
      using var probe = new Socket(SocketType.Stream, ProtocolType.Tcp);
      using var cts = new CancellationTokenSource(ProbeTimeoutMs);
      try
      {
        // .AsTask().GetAwaiter().GetResult() consumes the result synchronously, so
        // any fault/cancellation is observed here rather than escaping to the finalizer.
        probe.ConnectAsync(ip, port, cts.Token).AsTask().GetAwaiter().GetResult();
      }
      catch (Exception ex) when (ex is SocketException or OperationCanceledException)
      {
        throw new SocketException((int)SocketError.TimedOut);
      }
    }

    public void Load(IPluginSensorsContainer container)
    {
      var bindings = new List<RuleBinding>();

      foreach (var ruleConf in _config.Rules)
      {
        string safeId = string.IsNullOrWhiteSpace(ruleConf.Id)
            ? $"OPENRGB_{ruleConf.Name.Replace(" ", "_").ToUpper()}"
            : ruleConf.Id;

        var controlSensor = new OpenRgbControlSensor(safeId, ruleConf.Name);
        var binding = new RuleBinding(ruleConf, controlSensor);
        bindings.Add(binding);
        container.ControlSensors.Add(controlSensor);
        Log($"Card '{ruleConf.Name}' created. Regex '{ruleConf.DeviceRegex}' configured.", LogLevel.Info);
      }

      _bindings = bindings;
      _engine?.SetBindings(bindings);
      _nzxtRenderer?.SetBindings(bindings);
    }

    // Surfaces a terminal engine failure to the user via the host dialog. Marshaled
    // off the engine's timer thread: that thread is drained by Dispose(), so a modal
    // dialog on it would deadlock Close(). The inner catch keeps the fire-and-forget
    // Task from ever faulting unobserved.
    [ExcludeFromCodeCoverage]
    private void ShowFatal(string message) =>
      Task.Run(() =>
      {
        try { _dialog.ShowMessageDialog(message); }
        catch { /* host dialog unavailable; the same message is already logged */ }
      });

    private void Log(string message, LogLevel level = LogLevel.Info)
    {
      if (level >= _config.LogLevel)
      {
        string prefix = $"[{level.ToString().ToUpper()}]";
        _logger.Log($"[RGB] {prefix} {message}");
      }
    }

    public void Close()
    {
      _engine?.Dispose();
      _nzxtRenderer?.Dispose();
    }

    // Resolve the plugin's own folder (next to its DLL under Plugins\), not the
    // host's working directory, so the config sits beside the plugin.
    private static string PluginDirectory()
    {
      var dir = Path.GetDirectoryName(typeof(OpenRgbPlugin).Assembly.Location);
      return string.IsNullOrEmpty(dir) ? AppContext.BaseDirectory : dir;
    }
  }
}
