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

    private IOpenRgbBroker? _broker;
    private Timer? _renderTimer;
    private Device[] _devices = [];
    private Color[][]? _physicalBuffers;
    private volatile bool _isRendering;

    private OpenRgbConfig _config = new();
    private readonly string _configPath = Path.Combine(AppContext.BaseDirectory, "OpenRGBConfig.json");
    private readonly List<RuleBinding> _bindings = [];
    private int _frameCount = 0;
    private bool[] _deviceNeedsUpdate = [];

    private Stopwatch _startupStopwatch = new();

    // Test seam: inject config directly without touching disk or the OpenRGB server.
    internal OpenRgbPlugin(IPluginDialog dialog, IPluginLogger logger, OpenRgbConfig config)
        : this(dialog, logger) => _config = config;

    public void Initialize()
    {
      var loaded = ConfigLoader.LoadOrCreate(_configPath, dialog, Log);
      if (loaded == null) return; // template generated, nothing to drive yet
      _config = loaded;

      OpenRgbClient? client = null;
      try
      {
        client = new OpenRgbClient(name: "FanControl", ip: _config.ServerIp, port: _config.ServerPort);
        client.Connect();
        _broker = new OpenRgbBroker(client);
        _devices = _broker.GetAllControllerData();

        _physicalBuffers = new Color[_devices.Length][];
        _deviceNeedsUpdate = new bool[_devices.Length];
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
        // If ownership never transferred to the broker, the client would leak.
        if (_broker == null) client?.Dispose();
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
        if (File.Exists(LockFile.Path)) return;
        if (_broker == null || !_broker.Connected || _physicalBuffers == null) return;
        _frameCount++;
        RenderFrame(_broker, _devices, _physicalBuffers, _bindings, _config, _startupStopwatch, _deviceNeedsUpdate, _frameCount);
      }
      catch (Exception ex)
      {
        // Never let a render exception escape the timer thread: an unhandled
        // exception here terminates the host process. Degrade gracefully.
        Log($"Render frame failed: {ex.Message}", LogLevel.Error);
      }
      finally
      {
        _isRendering = false;
      }
    }

    internal static void RenderFrame(
        IOpenRgbBroker broker,
        Device[] devices,
        Color[][] physicalBuffers,
        List<RuleBinding> bindings,
        OpenRgbConfig config,
        Stopwatch startupStopwatch,
        bool[] deviceNeedsUpdate,
        int frameCount)
    {
      Array.Clear(deviceNeedsUpdate);

      // --- STARTUP ANIMATION ---
      if (config.Startup != null && config.Startup.Effect != null)
      {
        if (startupStopwatch.Elapsed.TotalSeconds < config.Startup.DurationSeconds)
        {
          config.Startup.Effect.Apply(devices, ".*", null, null, 100f, frameCount, config.TransitionSpeed, physicalBuffers);
          for (int i = 0; i < devices.Length; i++) broker.UpdateLeds(i, physicalBuffers[i]);
          return;
        }
        else if (startupStopwatch.IsRunning)
        {
          startupStopwatch.Stop();
          // startup complete, fall through to layer stack
        }
      }

      // --- LAYER STACKING ---
      foreach (var binding in bindings)
      {
        float val = binding.Control.Value ?? 0f;
        if (val < binding.Config.ActivationThreshold) continue;

        // Re-scale so effects always receive 0–100 relative to the activation range,
        // not the raw sensor value. A threshold of 75 means raw=75 → effect sees 0, raw=100 → 100.
        float range = 100f - binding.Config.ActivationThreshold;
        float valueToPass = range > 0 ? Math.Clamp(((val - binding.Config.ActivationThreshold) / range) * 100f, 0f, 100f) : 100f;

        float speedToUse = binding.Config.TransitionSpeed ?? config.TransitionSpeed;

        binding.Config.Effect?.Apply(
            devices,
            binding.Config.DeviceRegex,
            binding.Config.ZoneRegex,
            binding.Config.LedRegex,
            valueToPass,
            frameCount,
            speedToUse,
            physicalBuffers
        );

        for (int i = 0; i < devices.Length; i++)
        {
          if (binding.DeviceRegex.IsMatch(devices[i].Name ?? ""))
          {
            deviceNeedsUpdate[i] = true;
          }
        }
      }

      // --- OPTIMIZED HARDWARE COMMIT ---
      for (int i = 0; i < devices.Length; i++)
      {
        // Only push to USB if a layer was targeting this device.
        if (deviceNeedsUpdate[i])
        {
          broker.UpdateLeds(i, physicalBuffers[i]);
        }
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
      // Drain any in-flight tick before disposing the broker, otherwise a
      // running RenderFrame could touch a disposed OpenRGB connection.
      if (_renderTimer != null)
      {
        using var drained = new ManualResetEvent(false);
        _renderTimer.Dispose(drained);
        drained.WaitOne();
        _renderTimer = null;
      }
      _broker?.Dispose();
      _broker = null;
    }
  }
}
