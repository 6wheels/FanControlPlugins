using System;
using System.Text.RegularExpressions;
using OpenRGB.NET;

namespace FanControl.OpenRGB.Effects
{
  public class SpatialGradientEffect : BaseRgbEffect
  {
    public string ColorMinHex { get; set; } = "#00FF00"; // Color at the left edge of the gradient
    public string ColorMaxHex { get; set; } = "#0000FF"; // Color at the right edge of the gradient

    protected override void ProcessEffect(Device device, string? zoneRegex, string? ledRegex, float value, int frameCount, float transitionSpeed, Color[] buffer)
    {
      Color cMin = ParseHex(ColorMinHex);
      Color cMax = ParseHex(ColorMaxHex);

      float intensity = ModulateByValue ? Math.Clamp(value / 100f, 0.0f, 1.0f) : 1.0f;

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
                if (ledIndex != 0xFFFFFFFF && ledOffset + ledIndex < buffer.Length)
                {
                  string ledName = device.Leds[ledOffset + ledIndex].Name;
                  if (string.IsNullOrEmpty(ledRegex) || Regex.IsMatch(ledName, ledRegex))
                  {
                    // Determine the horizontal position ratio within the matrix row.
                    float ratio = width > 1 ? (float)x / (width - 1) : 0f;

                    Color gradColor = Interpolate(cMin, cMax, ratio);
                    Color targetColor = new(
                        (byte)(gradColor.R * intensity),
                        (byte)(gradColor.G * intensity),
                        (byte)(gradColor.B * intensity)
                    );

                    buffer[ledOffset + ledIndex] = LerpColor(buffer[ledOffset + ledIndex], targetColor, transitionSpeed);
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

                Color gradColor = Interpolate(cMin, cMax, ratio);
                Color targetColor = new(
                    (byte)(gradColor.R * intensity),
                    (byte)(gradColor.G * intensity),
                    (byte)(gradColor.B * intensity)
                );

                buffer[ledOffset + l] = LerpColor(buffer[ledOffset + l], targetColor, transitionSpeed);
              }
            }
          }
        }
        ledOffset += (int)zone.LedCount;
      }
    }

    private static Color Interpolate(Color c1, Color c2, float ratio)
    {
      byte r = (byte)(c1.R + (c2.R - c1.R) * ratio);
      byte g = (byte)(c1.G + (c2.G - c1.G) * ratio);
      byte b = (byte)(c1.B + (c2.B - c1.B) * ratio);
      return new Color(r, g, b);
    }
  }
}