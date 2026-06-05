namespace FanControl.OpenRGB.Rules
{
  public class RuleBinding(RuleConfig config, OpenRgbControlSensor control)
  {
    // The static configuration (from JSON)
    public RuleConfig Config { get; } = config;

    // The dynamic sensor (which lives in FanControl's UI)
    public OpenRgbControlSensor Control { get; } = control;
  }
}