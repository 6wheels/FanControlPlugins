using System.Text.RegularExpressions;
using OpenRGB.NET;

using FanControl.Rgb.Toolkit.Rendering;

namespace FanControl.Rgb.Effects
{
  public class RainbowEffect : BaseRgbEffect
  {
    public float Speed { get; set; } = 1.0f;      // relative speed, 0.0-10.0 (1.0 ≈ one rainbow every 4s)
    public float Spread { get; set; } = 1.0f;     // degrees of hue shift per LED, 0.0-360.0
    public float Saturation { get; set; } = 1.0f; // HSV saturation, 0.0-1.0
    public float StartHue { get; set; } = 0.0f;   // base hue offset in degrees, 0.0-360.0

    protected override void ProcessEffect(IRgbDevice device, string? zoneRegex, string? ledRegex, float value, int frameCount, int framerate, float transitionSpeed, Color[] buffer)
    {
      float brightness = ModulateByValue ? Math.Clamp(value / 100f, 0f, 1f) : 1f;
      // Convert frame index to seconds so Speed is cycles/sec regardless of framerate.
      float seconds = framerate > 0 ? frameCount / (float)framerate : 0f;
      float phase = seconds * Speed * ReferenceCyclesPerSecond * 360f + StartHue;
      float fade = NormalizeFade(transitionSpeed, framerate);

      int ledOffset = 0;
      foreach (var zone in device.Zones)
      {
        if (string.IsNullOrEmpty(zoneRegex) || Regex.IsMatch(zone.Name, zoneRegex))
        {
          if (zone.MatrixMap != null)
          {
            uint width = zone.MatrixMap.Width;
            uint height = zone.MatrixMap.Height;
            for (int y = 0; y < height; y++)
            {
              for (int x = 0; x < width; x++)
              {
                uint ledIndex = zone.MatrixMap.Matrix[y, x];
                if (ledIndex != 0xFFFFFFFF && ledOffset + ledIndex < buffer.Length)
                {
                  string ledName = device.Leds[ledOffset + (int)ledIndex].Name;
                  if (string.IsNullOrEmpty(ledRegex) || Regex.IsMatch(ledName, ledRegex))
                  {
                    float hue = ((phase + (int)ledIndex * Spread) % 360f + 360f) % 360f;
                    Color target = HsvToRgb(hue, Saturation, brightness);
                    buffer[ledOffset + ledIndex] = LerpColor(buffer[ledOffset + ledIndex], target, fade);
                  }
                }
              }
            }
          }
          else
          {
            for (int l = 0; l < zone.LedCount; l++)
            {
              string ledName = device.Leds[ledOffset + l].Name;
              if (string.IsNullOrEmpty(ledRegex) || Regex.IsMatch(ledName, ledRegex))
              {
                float hue = ((phase + l * Spread) % 360f + 360f) % 360f;
                Color target = HsvToRgb(hue, Saturation, brightness);
                buffer[ledOffset + l] = LerpColor(buffer[ledOffset + l], target, fade);
              }
            }
          }
        }
        ledOffset += (int)zone.LedCount;
      }
    }
  }
}
