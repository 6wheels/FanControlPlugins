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
    private readonly TimeProvider _time;
    private readonly Func<bool>? _suspended;

    private OpenRgbConfig _config = new();
    private IReadOnlyList<RuleBinding> _bindings = [];
    private OpenRgbEngine? _engine;

    public OpenRgbPlugin(IPluginDialog dialog, IPluginLogger logger)
    {
      _dialog = dialog;
      _logger = logger;
      _configPath = Path.Combine(AppContext.BaseDirectory, "OpenRGBConfig.json");
      _connect = Connect;
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
        Func<bool>? suspended = null)
        : this(dialog, logger)
    {
      _config = config;
      if (configPath != null) _configPath = configPath;
      if (connect != null) _connect = connect;
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
    }

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
    }

    private void Log(string message, LogLevel level = LogLevel.Info)
    {
      if (level >= _config.LogLevel)
      {
        string prefix = $"[{level.ToString().ToUpper()}]";
        _logger.Log($"[OpenRGB] {prefix} {message}");
      }
    }

    public void Close() => _engine?.Dispose();
  }
}
