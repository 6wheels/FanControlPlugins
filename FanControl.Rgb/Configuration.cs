using System.Text.Json.Serialization;
using FanControl.Rgb.Effects;

namespace FanControl.Rgb
{
  public enum LogLevel
  {
    Debug = 0,
    Info = 1,
    Warning = 2,
    Error = 3
  }

  public class OpenRgbConfig
  {
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public LogLevel LogLevel { get; set; } = LogLevel.Info;
    public string ServerIp { get; set; } = "127.0.0.1";
    public int ServerPort { get; set; } = 6742;

    private int _framerate = 30;
    public int Framerate
    {
      get => _framerate;
      set => _framerate = Math.Clamp(value, 1, 240);
    }
    public float TransitionSpeed { get; set; } = 0.1f;
    public StartupConfig? Startup { get; set; }
    public ReconnectConfig Reconnect { get; set; } = new();
    public NzxtConfig? Nzxt { get; set; }
    public List<RuleConfig> Rules { get; set; } = [];
  }

  // Command-sink configuration. NZXT RGB is driven through the LiquidCtl bridge
  // (same serialized HID queue as the fans) instead of OpenRGB.exe, to avoid USB
  // contention. Each target's channels are registered as synthetic devices in the
  // same targeting namespace the rules' DeviceRegex already searches.
  public class NzxtConfig
  {
    public bool Enabled { get; set; } = false;

    // Name of the LiquidCtl bridge's dedicated RGB named pipe (without the
    // \\.\pipe\ prefix). The fan client holds the primary pipe open, so RGB uses
    // its own; both feed the same serialized HID queue in the bridge.
    public string PipeName { get; set; } = "LiquidCtlPipeRgb";

    private int _refreshHz = 15;
    // Upper bound on command-sink pushes; the NZXT firmware drops rapid commands,
    // so this stays low. A buffer diff suppresses redundant frames on top of this.
    public int RefreshHz
    {
      get => _refreshHz;
      set => _refreshHz = Math.Clamp(value, 1, 30);
    }

    public List<NzxtTarget> Targets { get; set; } = [];
  }

  public class NzxtTarget
  {
    // Matched against each rule's DeviceRegex (synthetic device name).
    public string Name { get; set; } = string.Empty;

    // liquidctl device-match string passed to the bridge. Defaults to Name.
    private string? _deviceMatch;
    public string DeviceMatch
    {
      get => string.IsNullOrEmpty(_deviceMatch) ? Name : _deviceMatch;
      set => _deviceMatch = value;
    }

    public List<NzxtChannel> Channels { get; set; } = [];
  }

  public class NzxtChannel
  {
    // liquidctl channel name (ring/logo for Kraken, led1/led2 for Smart Device).
    // Also the synthetic zone name, so a rule's ZoneRegex can target one channel.
    public string Name { get; set; } = string.Empty;

    private int _ledCount = 1;
    public int LedCount
    {
      get => _ledCount;
      set => _ledCount = Math.Max(1, value);
    }
  }

  public class StartupConfig
  {
    public double DurationSeconds { get; set; } = 5.0;
    public BaseRgbEffect Effect { get; set; } = null!;
  }

  public class ReconnectConfig
  {
    private int _maxRetries = 5;
    public int MaxRetries
    {
      get => _maxRetries;
      set => _maxRetries = Math.Max(0, value);
    }

    private double _delaySeconds = 5.0;
    public double DelaySeconds
    {
      get => _delaySeconds;
      set => _delaySeconds = Math.Max(0.0, value);
    }
  }

  public class RuleConfig
  {
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = "Animation RGB";
    public string DeviceRegex { get; set; } = ".*";
    public string? ZoneRegex { get; set; }
    public string? LedRegex { get; set; }

    private float _activationThreshold = 0f;

    public float ActivationThreshold
    {
      get => _activationThreshold;
      set => _activationThreshold = Math.Clamp(value, 0f, 100f);
    }

    private float? _transitionSpeed = 0.1f;
    public float? TransitionSpeed
    {
      get => _transitionSpeed;
      set => _transitionSpeed = value.HasValue ? Math.Clamp(value.Value, 0f, 1f) : null;
    }

    public BaseRgbEffect Effect { get; set; } = null!;
  }
}