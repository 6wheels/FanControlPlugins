using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using FanControl.OpenRGB.Effects;
using OpenRGB.NET;

namespace FanControl.OpenRGB.Rules
{
  [JsonPolymorphic(TypeDiscriminatorPropertyName = "Type")]
  [JsonDerivedType(typeof(StaticColorRule), "Static")]
  [JsonDerivedType(typeof(ThermalGradientRule), "ThermalGradient")]
  [JsonDerivedType(typeof(ThresholdBlinkRule), "ThresholdBlink")]
  [JsonDerivedType(typeof(ProgressBarRule), "ProgressBar")]
  [JsonDerivedType(typeof(BreathingLoadRule), "BreathingLoad")]
  [JsonDerivedType(typeof(BouncingExplosionRule), "BouncingExplosion")]
  public abstract class BaseRgbRule
  {
    [JsonIgnore]
    public bool IsFinished { get; protected set; } = false;

    public void Apply(OpenRgbClient client, Device[] devices, string deviceRegex, string? zoneRegex, float currentValue, int frameCount)
    {
      if (IsFinished) return;

      for (int i = 0; i < devices.Length; i++)
      {
        var device = devices[i];
        if (Regex.IsMatch(device.Name ?? "", deviceRegex))
        {
          ProcessEffect(client, device, i, zoneRegex, currentValue, frameCount);
        }
      }
    }

    // Signature abstraite stricte : 6 paramètres
    protected abstract void ProcessEffect(OpenRgbClient client, Device device, int deviceIndex, string? zoneRegex, float value, int frameCount);

    // Signature utilitaire stricte : 5 paramètres
    protected static void ApplyToTargetLeds(Device device, string? zoneRegex, Color[] currentColors, Color targetColor, float fadeSpeed = 1.0f)
    {
      static Color LerpColor(Color current, Color target, float speed)
      {
        if (speed >= 1.0f) return target;
        byte r = (byte)(current.R + (target.R - current.R) * speed);
        byte g = (byte)(current.G + (target.G - current.G) * speed);
        byte b = (byte)(current.B + (target.B - current.B) * speed);
        return new Color(r, g, b);
      }

      if (string.IsNullOrEmpty(zoneRegex))
      {
        for (int i = 0; i < currentColors.Length; i++)
          currentColors[i] = LerpColor(currentColors[i], targetColor, fadeSpeed);
      }
      else
      {
        int ledOffset = 0;
        foreach (var zone in device.Zones)
        {
          if (Regex.IsMatch(zone.Name, zoneRegex))
          {
            for (int l = 0; l < zone.LedCount; l++)
              currentColors[ledOffset + l] = LerpColor(currentColors[ledOffset + l], targetColor, fadeSpeed);
          }
          ledOffset += (int)zone.LedCount;
        }
      }
    }
  }
}