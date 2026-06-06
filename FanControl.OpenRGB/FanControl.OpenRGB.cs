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
      if (File.Exists(_lockFilePath))
      {
        if (_client != null && _client.Connected)
        {
          _client.Dispose();
          _client = null;
          Log("FanControl plugin paused (DevKit mode active).", LogLevel.Info);
        }
        return;
      }
      else if (_client == null || !_client.Connected)
      {
        // Existing reconnection logic... (Keep your code here)
      }

      if (_client == null || !_client.Connected) return;

      _frameCount++;
      bool shouldLogThisFrame = (_frameCount % (_config.Framerate * 2) == 0);

      // ==========================================
      // 1. INITIALIZE VIRTUAL BUFFERS
      // ==========================================
      // Capture the current physical hardware state. This serves as the basis
      // for the Lerp (fade) to work smoothly across frames.
      Color[][] frameBuffers = new Color[_devices.Length][];
      for (int i = 0; i < _devices.Length; i++)
      {
        frameBuffers[i] = _client.GetControllerData(i).Colors;
      }

      // ==========================================
      // 2. STARTUP ANIMATION (Highest priority)
      // ==========================================
      if (_config.Startup != null && _config.Startup.Effect != null)
      {
        if (_startupStopwatch.Elapsed.TotalSeconds < _config.Startup.DurationSeconds)
        {
          _config.Startup.Effect.Apply(_client, _devices, ".*", null, null, 100f, _frameCount, _config.TransitionSpeed, frameBuffers);

          // Immediate hardware commit
          for (int i = 0; i < _devices.Length; i++) _client.UpdateLeds(i, frameBuffers[i]);
          return;
        }
        else if (_startupStopwatch.IsRunning)
        {
          _startupStopwatch.Stop();
          Log("Startup animation finished. Switching to layers.", LogLevel.Info);
        }
      }

      // ==========================================
      // 3. LAYER STACKING (Z-Index)
      // ==========================================
      foreach (var binding in _bindings)
      {
        float val = binding.Control.Value ?? 0f;

        // If the sensor is below threshold, this layer is INVISIBLE (transparent).
        // Skip directly to the next layer.
        if (val < binding.Config.ActivationThreshold) continue;

        // If active, remap the value from threshold to 0-100%
        // starting at the moment it crosses the threshold.
        float valueToPass;
        float range = 100f - binding.Config.ActivationThreshold;
        if (range > 0)
        {
          valueToPass = Math.Clamp(((val - binding.Config.ActivationThreshold) / range) * 100f, 0f, 100f);
        }
        else
        {
          valueToPass = 100f;
        }

        float speedToUse = binding.Config.TransitionSpeed ?? _config.TransitionSpeed;

        if (shouldLogThisFrame)
        {
          Log($"Layer '{binding.Config.Name}' | Val: {val:F1} (Mapped: {valueToPass:F1}) | Threshold: {binding.Config.ActivationThreshold}", LogLevel.Debug);
        }

        // Apply the effect to our virtual in-memory buffers
        binding.Config.Effect?.Apply(
            _client,
            _devices,
            binding.Config.DeviceRegex,
            binding.Config.ZoneRegex,
            binding.Config.LedRegex,
            valueToPass,
            _frameCount,
            speedToUse,
            frameBuffers
        );
      }

      // ==========================================
      // 4. HARDWARE COMMIT (ONE USB CALL PER DEVICE)
      // ==========================================
      for (int i = 0; i < _devices.Length; i++)
      {
        _client.UpdateLeds(i, frameBuffers[i]);
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