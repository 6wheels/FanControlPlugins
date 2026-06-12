using System.Text.RegularExpressions;

namespace FanControl.OpenRGB.Rules
{
  public class RuleBinding(RuleConfig config, OpenRgbControlSensor control)
  {
    public RuleConfig Config { get; } = config;
    public OpenRgbControlSensor Control { get; } = control;
    public Regex DeviceRegex { get; } = new Regex(config.DeviceRegex, RegexOptions.Compiled);
  }
}