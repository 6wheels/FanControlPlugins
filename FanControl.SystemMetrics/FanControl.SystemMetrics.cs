using FanControl.Plugins;
using FanControl.SystemMetrics.Toolkit;

namespace FanControl.SystemMetrics
{
  // Orchestrator: loads the config, then registers only the metrics the user enabled.
  // Sensor definitions live in MetricCatalog; config IO lives in ConfigLoader.
  public class MetricsPlugin : IPlugin
  {
    public string Name => "System Metrics";

    private readonly IPluginDialog _dialog;
    private readonly IPluginLogger _logger;
    private readonly string _configPath;

    private SystemMetricsConfig? _config;

    public MetricsPlugin(IPluginDialog dialog, IPluginLogger logger)
    {
      _dialog = dialog;
      _logger = logger;
      _configPath = Path.Combine(AppContext.BaseDirectory, "SystemMetricsConfig.json");
    }

    // Test seam: inject config and bypass the real config path.
    internal MetricsPlugin(IPluginDialog dialog, IPluginLogger logger, SystemMetricsConfig config, string? configPath = null)
        : this(dialog, logger)
    {
      _config = config;
      if (configPath != null) _configPath = configPath;
    }

    public void Initialize() => _config = ConfigLoader.LoadOrCreate(_configPath, _dialog, Log);

    public void Load(IPluginSensorsContainer container)
    {
      if (_config is null) return; // template generated this run; nothing to register

      foreach (MetricDefinition def in MetricCatalog.All)
      {
        if (_config.EnabledMetrics.Contains(def.Key, StringComparer.OrdinalIgnoreCase))
        {
          // Injection into the temperature pool to bypass the UI filter
          container.TempSensors.Add(def.CreateSensor());
          Log($"Metric '{def.Key}' enabled.");
        }
      }
    }

    private void Log(string message) => _logger.Log($"[SystemMetrics] {message}");

    public void Close() { }
  }
}
