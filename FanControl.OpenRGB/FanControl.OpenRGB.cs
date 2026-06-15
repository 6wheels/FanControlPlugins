using System.Diagnostics.CodeAnalysis;
using FanControl.Plugins;
using OpenRGB.NET;
using FanControl.OpenRGB.Rules;
using FanControl.OpenRGB.Toolkit;

namespace FanControl.OpenRGB
{
  // Orchestrator: owns config + sensor bindings and the lifecycle of the
  // OpenRgbEngine, which holds all RGB/threading/state-machine concerns.
  public class OpenRgbPlugin : IPlugin
  {
    public string Name => "OpenRGB";

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
      _configPath = Path.Combine(PluginDirectory(), "OpenRGBConfig.json");
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

      _engine = new OpenRgbEngine(_config, _connect, Log, _time, _suspended);
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
    [ExcludeFromCodeCoverage]
    private static IOpenRgbBroker Connect(OpenRgbConfig config)
    {
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

    private void Log(string message, LogLevel level = LogLevel.Info)
    {
      if (level >= _config.LogLevel)
      {
        string prefix = $"[{level.ToString().ToUpper()}]";
        _logger.Log($"[OpenRGB] {prefix} {message}");
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
