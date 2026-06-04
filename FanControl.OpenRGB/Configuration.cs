using System.Text.Json.Serialization;

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
    public int Framerate { get; set; } = 30; // 30 FPS par défaut

    public List<RuleConfig> Rules { get; set; } = [];
  }

  public class RuleConfig
  {
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = "Animation RGB";
    public string DeviceRegex { get; set; } = ".*";
    public string? ZoneRegex { get; set; }

    public float ActivationThreshold { get; set; } = 0f;

    public Rules.BaseRgbRule MainRule { get; set; } = null!;
    public Rules.BaseRgbRule? IdleRule { get; set; }
  }
}