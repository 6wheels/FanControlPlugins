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
  public abstract class BaseRgbEffect
  {
    [JsonIgnore]
    public bool IsFinished { get; protected set; } = false;

    public void Apply(OpenRgbClient client, Device[] devices, string deviceRegex, string? zoneRegex, string? ledRegex, float currentValue, int frameCount, float transitionSpeed)
    {
      if (IsFinished) return;

      for (int i = 0; i < devices.Length; i++)
      {
        var device = devices[i];
        if (Regex.IsMatch(device.Name ?? "", deviceRegex))
        {
          ProcessEffect(client, device, i, zoneRegex, ledRegex, currentValue, frameCount, transitionSpeed);
        }
      }
    }

    // Strict abstract signature: 6 parameters
    protected abstract void ProcessEffect(OpenRgbClient client, Device device, int deviceIndex, string? zoneRegex, string? ledRegex, float value, int frameCount, float transitionSpeed);

    // Strict utility signature: 5 parameters
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
  }
}