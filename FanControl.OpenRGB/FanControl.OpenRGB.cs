using System.Text.Json;
using System.Text.RegularExpressions;
using FanControl.Plugins;
using OpenRGB.NET;
using FanControl.OpenRGB.Rules;
using FanControl.OpenRGB.Effects;
using FanControl.OpenRGB.Toolkit;
using System.Diagnostics;

namespace FanControl.OpenRGB
{
  public class OpenRgbPlugin(IPluginDialog dialog, IPluginLogger logger) : IPlugin
  {
    public string Name => "OpenRGB";

    private OpenRgbClient? _client;
    private Timer? _renderTimer;
    private Device[] _devices = [];
    private Color[][]? _physicalBuffers;
    private bool _isRendering = false;

    private OpenRgbConfig _config = new();
    private readonly string _configPath = Path.Combine(AppContext.BaseDirectory, "OpenRGBConfig.json");
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

        _physicalBuffers = new Color[_devices.Length][];
        Log($"Connected. {_devices.Length} detected devices:");
        for (int i = 0; i < _devices.Length; i++)
        {
          Log($"Device [{i}] : '{_devices[i].Name}', Available Zones: {_devices[i].Zones})");
          _physicalBuffers[i] = _devices[i].Colors;
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
        var binding = new RuleBinding(ruleConf, controlSensor);
        _bindings.Add(binding);
        container.ControlSensors.Add(controlSensor);
        int matchCount = _devices.Count(d => Regex.IsMatch(d.Name ?? "", ruleConf.DeviceRegex));
        Log($"Card '{ruleConf.Name}' created. Regex '{ruleConf.DeviceRegex}' matches {matchCount} device(s).", LogLevel.Info);
      }
    }

    private void RenderLoop_Tick(object? state)
    {
      // Reentrancy guard: timer fires every ~33ms and USB commits can be slow.
      if (_isRendering) return;
      _isRendering = true;

      try
      {
        if (File.Exists(LockFile.Path)) return; // Pause DevKit
        if (_client == null || !_client.Connected || _physicalBuffers == null) return;

        _frameCount++;

        // Track which devices are touched this frame to avoid redundant USB writes.
        bool[] deviceNeedsUpdate = new bool[_devices.Length];

        // --- STARTUP ANIMATION ---
        if (_config.Startup != null && _config.Startup.Effect != null)
        {
          if (_startupStopwatch.Elapsed.TotalSeconds < _config.Startup.DurationSeconds)
          {
            _config.Startup.Effect.Apply(_devices, ".*", null, null, 100f, _frameCount, _config.TransitionSpeed, _physicalBuffers);
            for (int i = 0; i < _devices.Length; i++) _client.UpdateLeds(i, _physicalBuffers[i]);
            return;
          }
          else if (_startupStopwatch.IsRunning)
          {
            _startupStopwatch.Stop();
            Log("Startup animation complete. Switching to layer stack.", LogLevel.Info);
          }
        }

        // --- LAYER STACKING ---
        foreach (var binding in _bindings)
        {
          float val = binding.Control.Value ?? 0f;
          if (val < binding.Config.ActivationThreshold) continue;

          // Re-scale so effects always receive 0–100 relative to the activation range,
          // not the raw sensor value. A threshold of 75 means raw=75 → effect sees 0, raw=100 → 100.
          float range = 100f - binding.Config.ActivationThreshold;
          float valueToPass = range > 0 ? Math.Clamp(((val - binding.Config.ActivationThreshold) / range) * 100f, 0f, 100f) : 100f;

          float speedToUse = binding.Config.TransitionSpeed ?? _config.TransitionSpeed;

          binding.Config.Effect?.Apply(
              _devices,
              binding.Config.DeviceRegex,
              binding.Config.ZoneRegex,
              binding.Config.LedRegex,
              valueToPass,
              _frameCount,
              speedToUse,
              _physicalBuffers
          );

          for (int i = 0; i < _devices.Length; i++)
          {
            if (Regex.IsMatch(_devices[i].Name ?? "", binding.Config.DeviceRegex))
            {
              deviceNeedsUpdate[i] = true;
            }
          }
        }

        // --- OPTIMIZED HARDWARE COMMIT ---
        for (int i = 0; i < _devices.Length; i++)
        {
          // Only push to USB if a layer was targeting this device.
          if (deviceNeedsUpdate[i])
          {
            _client.UpdateLeds(i, _physicalBuffers[i]);
          }
        }
      }
      finally
      {
        // Always release the lock to allow the next frame.
        _isRendering = false;
      }
    }

    private void Log(string message, LogLevel level = LogLevel.Info)
    {
      if (level >= _config.LogLevel)
      {
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