using System.Text.RegularExpressions;
using OpenRGB.NET;

namespace FanControl.OpenRGB.Effects
{
  public class RainbowEffect : BaseRgbEffect
  {
    public float Speed { get; set; } = 1.0f;
    public float Spread { get; set; } = 1.0f;

    protected override void ProcessEffect(Device device, string? zoneRegex, string? ledRegex, float value, int frameCount, float transitionSpeed, Color[] buffer)
    {
      float brightness = ModulateByValue ? Math.Clamp(value / 100f, 0f, 1f) : 1f;

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
                  string ledName = device.Leds[ledOffset + ledIndex].Name;
                  if (string.IsNullOrEmpty(ledRegex) || Regex.IsMatch(ledName, ledRegex))
                  {
                    float hue = ((frameCount * Speed + (int)ledIndex * Spread) % 360f + 360f) % 360f;
                    Color target = HsvToRgb(hue, 1f, brightness);
                    buffer[ledOffset + ledIndex] = LerpColor(buffer[ledOffset + ledIndex], target, transitionSpeed);
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
                float hue = ((frameCount * Speed + l * Spread) % 360f + 360f) % 360f;
                Color target = HsvToRgb(hue, 1f, brightness);
                buffer[ledOffset + l] = LerpColor(buffer[ledOffset + l], target, transitionSpeed);
              }
            }
          }
        }
        ledOffset += (int)zone.LedCount;
      }
    }

    private static Color HsvToRgb(float h, float s, float v)
    {
      float sector = h / 60f;
      int i = (int)Math.Floor(sector) % 6;
      float f = sector - (float)Math.Floor(sector);
      float p = v * (1f - s);
      float q = v * (1f - f * s);
      float t = v * (1f - (1f - f) * s);
      return i switch
      {
        0 => new Color((byte)(v * 255), (byte)(t * 255), (byte)(p * 255)),
        1 => new Color((byte)(q * 255), (byte)(v * 255), (byte)(p * 255)),
        2 => new Color((byte)(p * 255), (byte)(v * 255), (byte)(t * 255)),
        3 => new Color((byte)(p * 255), (byte)(q * 255), (byte)(v * 255)),
        4 => new Color((byte)(t * 255), (byte)(p * 255), (byte)(v * 255)),
        _ => new Color((byte)(v * 255), (byte)(p * 255), (byte)(q * 255)),
      };
    }
  }
}
