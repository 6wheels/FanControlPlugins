using System;
using System.Text.RegularExpressions;
using OpenRGB.NET;

using FanControl.Rgb.Toolkit.Rendering;

namespace FanControl.Rgb.Effects
{
  public class SpatialGradientEffect : BaseRgbEffect
  {
    // 2-stop fallback used when ColorStops is empty.
    public string ColorMinHex { get; set; } = "#00FF00";
    public string ColorMaxHex { get; set; } = "#0000FF";

    // Optional multi-stop gradient. When non-empty it overrides ColorMinHex/ColorMaxHex.
    // Stop positions are 0.0-1.0 mapped across the LEDs of each zone.
    public List<GradientStop> ColorStops { get; set; } = new();

    protected override void ProcessEffect(IRgbDevice device, string? zoneRegex, string? ledRegex, float value, int frameCount, int framerate, float transitionSpeed, LedWriter writer)
    {
      var stops = ResolveStops(ColorStops, ColorMinHex, ColorMaxHex);

      float intensity = ModulateByValue ? Math.Clamp(value / 100f, 0.0f, 1.0f) : 1.0f;
      float fade = NormalizeFade(transitionSpeed, framerate);

      int ledOffset = 0;
      foreach (var zone in device.Zones)
      {
        if (string.IsNullOrEmpty(zoneRegex) || Regex.IsMatch(zone.Name, zoneRegex))
        {
          // === 2D MODE (keyboard or other matrix-based device) ===
          if (zone.MatrixMap != null)
          {
            uint width = zone.MatrixMap.Width;
            uint height = zone.MatrixMap.Height;

            for (int y = 0; y < height; y++)
            {
              for (int x = 0; x < width; x++)
              {
                uint ledIndex = zone.MatrixMap.Matrix[y, x];
                if (ledIndex != 0xFFFFFFFF && ledOffset + ledIndex < writer.Length)
                {
                  string ledName = device.Leds[ledOffset + (int)ledIndex].Name;
                  if (string.IsNullOrEmpty(ledRegex) || Regex.IsMatch(ledName, ledRegex))
                  {
                    float ratio = width > 1 ? (float)x / (width - 1) : 0f;

                    Color gradColor = SampleGradient(stops, ratio);
                    Color targetColor = new(
                        (byte)(gradColor.R * intensity),
                        (byte)(gradColor.G * intensity),
                        (byte)(gradColor.B * intensity)
                    );

                    writer.Write((int)(ledOffset + ledIndex), targetColor, fade);
                  }
                }
              }
            }
          }
          // === 1D MODE (LED strips, fans, RAM lighting) ===
          else
          {
            for (int l = 0; l < zone.LedCount; l++)
            {
              string ledName = device.Leds[ledOffset + l].Name;
              if (string.IsNullOrEmpty(ledRegex) || Regex.IsMatch(ledName, ledRegex))
              {
                float ratio = zone.LedCount > 1 ? (float)l / (zone.LedCount - 1) : 0f;

                Color gradColor = SampleGradient(stops, ratio);
                Color targetColor = new(
                    (byte)(gradColor.R * intensity),
                    (byte)(gradColor.G * intensity),
                    (byte)(gradColor.B * intensity)
                );

                writer.Write(ledOffset + l, targetColor, fade);
              }
            }
          }
        }
        ledOffset += (int)zone.LedCount;
      }
    }
  }
}