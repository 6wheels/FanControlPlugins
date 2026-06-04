namespace FanControl.OpenRGB.Rules
{
  public class RuleBinding
  {
    public string DeviceRegex { get; set; } = ".*";
    public string? ZoneRegex { get; set; }

    public OpenRgbControlSensor Control { get; set; } = null!; // Notre nouvelle carte !
    public float ActivationThreshold { get; set; } = 0f;

    public BaseRgbRule MainRule { get; set; } = null!;
    public BaseRgbRule? IdleRule { get; set; }
  }
}