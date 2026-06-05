namespace FanControl.OpenRGB.Rules
{
  public class RuleBinding(RuleConfig config, OpenRgbControlSensor control)
  {
    // La configuration statique (issue du JSON)
    public RuleConfig Config { get; } = config;

    // Le capteur dynamique (qui vit dans l'UI de FanControl)
    public OpenRgbControlSensor Control { get; } = control;
  }
}