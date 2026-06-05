using System.Text.Json.Serialization;
using FanControl.OpenRGB.Effects;

namespace FanControl.OpenRGB
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
    public int Framerate { get; set; } = 30;

    public StartupConfig? Startup { get; set; }
    public List<RuleConfig> Rules { get; set; } = [];
  }

  public class StartupConfig
  {
    public double DurationSeconds { get; set; } = 5.0;
    public BaseRgbEffect Effect { get; set; } = null!;
  }

  public class RuleConfig
  {
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = "Animation RGB";
    public string DeviceRegex { get; set; } = ".*";
    public string? ZoneRegex { get; set; }

    private float _activationThreshold = 0f;

    public float ActivationThreshold
    {
      get => _activationThreshold;
      set => _activationThreshold = Math.Clamp(value, 0f, 100f);
    }

    public BaseRgbEffect ActiveEffect { get; set; } = null!;
    public BaseRgbEffect? IdleEffect { get; set; }
  }
}