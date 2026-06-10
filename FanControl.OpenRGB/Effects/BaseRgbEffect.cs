using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using OpenRGB.NET;

namespace FanControl.OpenRGB.Effects
{
  [JsonPolymorphic(TypeDiscriminatorPropertyName = "Type")]
  [JsonDerivedType(typeof(StaticEffect), "Static")]
  [JsonDerivedType(typeof(GradientEffect), "Gradient")]
  [JsonDerivedType(typeof(BlinkEffect), "Blink")]
  [JsonDerivedType(typeof(BreathingEffect), "Breathing")]
  [JsonDerivedType(typeof(AuroraEffect), "Aurora")]
  [JsonDerivedType(typeof(SpatialGradientEffect), "SpatialGradient")]
  [JsonDerivedType(typeof(ProgressBarEffect), "ProgressBar")]
  [JsonDerivedType(typeof(GaugeGradientEffect), "GaugeGradient")]
  /// <summary>
  /// Base class for all RGB effects used by the FanControl OpenRGB plugin and the developer toolkit.
  /// </summary>
  public abstract class BaseRgbEffect
  {
    /// <summary>
    /// If true, the effect will use the sensor value (0-100) as a modulation factor.
    /// </summary>
    public bool ModulateByValue { get; set; } = true;

    [JsonIgnore]
    public bool IsFinished { get; protected set; } = false;

    /// <summary>
    /// Runs the effect on every device whose name matches the configured device regex.
    /// </summary>
    public void Apply(Device[] devices, string deviceRegex, string? zoneRegex, string? ledRegex, float currentValue, int frameCount, float transitionSpeed, Color[][] frameBuffers)
    {
      if (IsFinished) return;
      for (int i = 0; i < devices.Length; i++)
      {
        var device = devices[i];
        if (Regex.IsMatch(device.Name ?? "", deviceRegex))
        {
          ProcessEffect(device, zoneRegex, ledRegex, currentValue, frameCount, transitionSpeed, frameBuffers[i]);
        }
      }
    }

    protected abstract void ProcessEffect(Device device, string? zoneRegex, string? ledRegex, float value, int frameCount, float transitionSpeed, Color[] buffer);

    protected static void ApplyToTargetLeds(Device device, string? zoneRegex, string? ledRegex, Color[] currentColors, Color targetColor, float fadeSpeed = 1.0f)
    {
      static Color LerpColor(Color current, Color target, float speed)
      {
        if (speed <= 0f || speed >= 1.0f) return target;

        byte r = (byte)(current.R + (target.R - current.R) * speed);
        byte g = (byte)(current.G + (target.G - current.G) * speed);
        byte b = (byte)(current.B + (target.B - current.B) * speed);
        return new Color(r, g, b);
      }

      int ledOffset = 0;
      foreach (var zone in device.Zones)
      {
        if (string.IsNullOrEmpty(zoneRegex) || Regex.IsMatch(zone.Name, zoneRegex))
        {
          for (int l = 0; l < zone.LedCount; l++)
          {
            string ledName = device.Leds[ledOffset + l].Name;
            if (string.IsNullOrEmpty(ledRegex) || Regex.IsMatch(ledName, ledRegex))
            {
              currentColors[ledOffset + l] = LerpColor(currentColors[ledOffset + l], targetColor, fadeSpeed);
            }
          }
        }
        ledOffset += (int)zone.LedCount;
      }
    }

    protected static Color LerpColor(Color a, Color b, float t)
    {
      t = Math.Clamp(t, 0f, 1f);
      return new Color((byte)(a.R + (b.R - a.R) * t), (byte)(a.G + (b.G - a.G) * t), (byte)(a.B + (b.B - a.B) * t));
    }

    protected static Color ParseHex(string hex)
    {
      if (string.IsNullOrEmpty(hex)) return new Color(255, 255, 255);

      hex = hex.Replace("#", "");
      if (hex.Length != 6) return new Color(255, 255, 255);

      return new Color(
          Convert.ToByte(hex[..2], 16),
          Convert.ToByte(hex.Substring(2, 2), 16),
          Convert.ToByte(hex.Substring(4, 2), 16)
      );
    }
  }
}