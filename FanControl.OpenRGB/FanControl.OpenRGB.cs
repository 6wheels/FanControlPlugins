using System.Text.Json;
using System.Text.RegularExpressions;
using FanControl.Plugins;
using OpenRGB.NET;
using FanControl.OpenRGB.Rules;
using FanControl.OpenRGB.Effects;
using System.Diagnostics;

namespace FanControl.OpenRGB
{
  public class OpenRgbPlugin(IPluginDialog dialog, IPluginLogger logger) : IPlugin
  {
    public string Name => "OpenRGB";

    private OpenRgbClient? _client;
    private Timer? _renderTimer;
    private Device[] _devices = [];

    private OpenRgbConfig _config = new();
    private readonly string _configPath = "OpenRGBConfig.json";
    private readonly string _lockFilePath = Path.Combine(Path.GetTempPath(), "fancontrol_rgb.lock");
    private bool _isLocked = false;
    private readonly List<RuleBinding> _bindings = [];
    private int _frameCount = 0;

    private Stopwatch _startupStopwatch = new();

    public void Initialize()
    {
      if (File.Exists(_configPath))
      {
        try
        {
          var options = new JsonSerializerOptions { ReadCommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true };
          string json = File.ReadAllText(_configPath);
          _config = JsonSerializer.Deserialize<OpenRgbConfig>(json, options) ?? new OpenRgbConfig();

          Log($"Configuration loaded. {_config.Rules.Count} rule(s) found.", LogLevel.Info);

          string parsedConfigJson = JsonSerializer.Serialize(_config, new JsonSerializerOptions { WriteIndented = true });
          Log($"Parsed configuration dump:\n{parsedConfigJson}", LogLevel.Debug);
          // -------------------------------------------------
          Log($"Server IP: {_config.ServerIp}, Port: {_config.ServerPort}, Framerate: {_config.Framerate} FPS, LogLevel: {_config.LogLevel}", LogLevel.Debug);
        }
        catch (Exception ex)
        {
          Log($"Configuration failed: {ex.Message}", LogLevel.Error);
        }
      }
      else
      {
        File.WriteAllText(_configPath, JsonSerializer.Serialize(new OpenRgbConfig(), new JsonSerializerOptions { WriteIndented = true }));
        Log("Template configuration file generated. Please configure it.", LogLevel.Info);
        dialog.ShowMessageDialog("OpenRGB: Template configuration file generated. Please configure it.");
        return;
      }

      try
      {
        _client = new OpenRgbClient(name: "FanControl", ip: _config.ServerIp, port: _config.ServerPort);
        _client.Connect();
        _devices = _client.GetAllControllerData();

        // LOG 1: DEVICE INVENTORY
        Log($"Connected. {_devices.Length} detected devices:");
        for (int i = 0; i < _devices.Length; i++)
        {
          Log($"Device [{i}] : '{_devices[i].Name}', Available Zones: {_devices[i].Zones})");
        }

        if (_config.Startup != null && _config.Startup.Effect != null)
        {
          _startupStopwatch.Start();
          Log($"Startup animation started for {_config.Startup.DurationSeconds} seconds.", LogLevel.Info);
        }

        int interval = 1000 / _config.Framerate;
        _renderTimer = new Timer(RenderLoop_Tick, null, 0, interval);
      }
      catch (Exception ex)
      {
        Log($"Connection failed: {ex.Message}", LogLevel.Error);
      }
    }

    public void Load(IPluginSensorsContainer container)
    {
      _bindings.Clear();

      foreach (var ruleConf in _config.Rules)
      {
        string safeId = string.IsNullOrWhiteSpace(ruleConf.Id)
            ? $"OPENRGB_{ruleConf.Name.Replace(" ", "_").ToUpper()}"
            : ruleConf.Id;

        var controlSensor = new OpenRgbControlSensor(safeId, ruleConf.Name);

        // Much cleaner: we just pass the config object and the sensor
        var binding = new RuleBinding(ruleConf, controlSensor);

        _bindings.Add(binding);
        container.ControlSensors.Add(controlSensor);
        int matchCount = _devices.Count(d => Regex.IsMatch(d.Name ?? "", ruleConf.DeviceRegex));
        Log($"Card '{ruleConf.Name}' created. Regex '{ruleConf.DeviceRegex}' matches {matchCount} device(s).", LogLevel.Info);
      }
    }

    private void RenderLoop_Tick(object? state)
    {
      // --- LOCK MANAGEMENT (DEV TOOLKIT) ---
      bool lockExists = File.Exists(_lockFilePath);

      if (lockExists)
      {
        if (!_isLocked)
        {
          _isLocked = true;
          Log("Dev Toolkit detected (Lock file). Plugin is paused...", LogLevel.Warning);
        }
        return; // We completely cancel the execution of this frame
      }
      else if (_isLocked)
      {
        _isLocked = false;
        Log("Dev Toolkit closed. RGB control resumed by FanControl.", LogLevel.Info);
      }
      // ---------------------------------------------

      // We ensure we are properly connected before continuing
      if (_client == null || !_client.Connected) return;

      _frameCount++;

      if (_config.Startup != null && _config.Startup.Effect != null)
      {
        if (_startupStopwatch.Elapsed.TotalSeconds < _config.Startup.DurationSeconds)
        {
          // Apply the startup effect with an arbitrary value of 100f and a Fade of 1.0f
          _config.Startup.Effect.Apply(_client, _devices, ".*", null, null, 100f, _frameCount, _config.TransitionSpeed);
          return; // Stop the RenderLoop_Tick here, normal rules are not read
        }
        else if (_startupStopwatch.IsRunning)
        {
          _startupStopwatch.Stop();
          Log("Startup animation completed. Switching to standard monitoring.", LogLevel.Info);
        }
      }

      // We log only 1 frame every 2 seconds (if 30 FPS)
      bool shouldLogThisFrame = (_frameCount % (_config.Framerate * 2) == 0);

      foreach (var binding in _bindings)
      {
        float val = binding.Control.Value ?? 0f;

        BaseRgbEffect? effectToApply = null;

        if (val >= binding.Config.ActivationThreshold)
        {
          effectToApply = binding.Config.ActiveEffect;
        }
        else if (binding.Config.IdleEffect != null)
        {
          effectToApply = binding.Config.IdleEffect;
        }

        if (shouldLogThisFrame)
        {
          Log($"Card '{binding.Control.Name}' | Value: {val:F1} | Threshold: {binding.Config.ActivationThreshold} | Active Rule: {binding.Config.Name}", LogLevel.Debug);
        }

        float speedToUse = binding.Config.TransitionSpeed ?? _config.TransitionSpeed;

        effectToApply?.Apply(
            _client,
            _devices,
            binding.Config.DeviceRegex,
            binding.Config.ZoneRegex,
            binding.Config.LedRegex,
            val,
            _frameCount,
            speedToUse
        );
      }
    }

    private void Log(string message, LogLevel level = LogLevel.Info)
    {
      // We write to the FanControl log only if the message level is greater than or equal 
      // to the level requested in the config.
      if (level >= _config.LogLevel)
      {
        // We add a visual prefix if it is an error or debug
        string prefix = $"[{level.ToString().ToUpper()}]";

        // 'logger' comes from the primary constructor of your class
        logger.Log($"[OpenRGB] {prefix} {message}");
      }
    }

    public void Close()
    {
      _renderTimer?.Dispose();
      _client?.Dispose();
    }
  }
}