using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using OpenRGB.NET;

namespace FanControl.OpenRGB.Effects
{
  [JsonConverter(typeof(JsonStringEnumConverter))]
  public enum AuroraDirection
  {
    Horizontal,
    Vertical
  }

  public class AuroraEffect : BaseRgbEffect
  {
    public string Color1Hex { get; set; } = "#00FF66";
    public string Color2Hex { get; set; } = "#00FFFF";
    public string Color3Hex { get; set; } = "#9900FF";

    private float _speed = 0.05f;
    public float Speed
    {
      get => _speed;
      set => _speed = Math.Clamp(value, 0.001f, 2.0f); // Locked between very slow and very fast
    }

    private float _scale = 0.3f;
    public float Scale
    {
      get => _scale;
      set => _scale = Math.Clamp(value, 0.01f, 5.0f); // Prevents a scale of 0 that would crush the wave
    }

    public AuroraDirection Direction { get; set; } = AuroraDirection.Horizontal;

    protected override void ProcessEffect(OpenRgbClient client, Device device, int deviceIndex, string? zoneRegex, string? ledRegex, float value, int frameCount, float transitionSpeed, Color[] buffer)
    {
      Color c1 = ParseHex(Color1Hex);
      Color c2 = ParseHex(Color2Hex);
      Color c3 = ParseHex(Color3Hex);

      float time = frameCount * Speed;
      bool isVertical = Direction == AuroraDirection.Vertical;

      // Compute the dynamic intensity (0.0 to 1.0) based on the FanControl sensor
      float intensity = ModulateByValue ? Math.Clamp(value / 100f, 0.0f, 1.0f) : 1.0f;

      int ledOffset = 0;
      foreach (var zone in device.Zones)
      {
        // Zone filter (ex: "Keyboard")
        if (string.IsNullOrEmpty(zoneRegex) || Regex.IsMatch(zone.Name, zoneRegex))
        {
          // === 2D Device ===
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

                  // LED filter (ex: "^(Key: W|Key: A)$")
                  if (string.IsNullOrEmpty(ledRegex) || Regex.IsMatch(ledName, ledRegex))
                  {
                    // Compute the wave color mathematically
                    Color waveColor = CalculateAuroraColor(x, y, time, c1, c2, c3, isVertical);

                    // Apply the intensity
                    Color targetColor = new Color(
                        (byte)(waveColor.R * intensity),
                        (byte)(waveColor.G * intensity),
                        (byte)(waveColor.B * intensity)
                    );

                    // Application de la transition (Fade) pour s'enchaîner doucement avec l'état précédent
                    buffer[ledOffset + ledIndex] = LerpColor(buffer[ledOffset + ledIndex], targetColor, transitionSpeed);
                  }
                }
              }
            }
          }
          // === 1D Device ===
          else
          {
            for (int l = 0; l < zone.LedCount; l++)
            {
              string ledName = device.Leds[ledOffset + l].Name;

              if (string.IsNullOrEmpty(ledRegex) || Regex.IsMatch(ledName, ledRegex))
              {
                Color waveColor = CalculateAuroraColor(l, 0, time, c1, c2, c3, isVertical);

                Color targetColor = new(
                    (byte)(waveColor.R * intensity),
                    (byte)(waveColor.G * intensity),
                    (byte)(waveColor.B * intensity)
                );

                buffer[ledOffset + l] = LerpColor(buffer[ledOffset + l], targetColor, transitionSpeed);
              }
            }
          }
        }
        ledOffset += (int)zone.LedCount;
      }
    }

    private Color CalculateAuroraColor(int x, int y, float time, Color c1, Color c2, Color c3, bool isVertical)
    {
      double mainWave, secondaryWave;

      // Axes inversion according to the chosen direction
      if (isVertical)
      {
        mainWave = Math.Sin(y * Scale + time);
        secondaryWave = Math.Sin(x * Scale - time * 0.7f);
      }
      else
      {
        mainWave = Math.Sin(x * Scale + time);
        secondaryWave = Math.Sin(y * Scale - time * 0.7f);
      }

      float factor = (float)((mainWave + secondaryWave + 2.0) / 4.0);

      if (factor < 0.5f) return LerpColor(c1, c2, factor * 2f);
      else return LerpColor(c2, c3, (factor - 0.5f) * 2f);
    }

    private Color LerpColor(Color a, Color b, float t)
    {
      t = Math.Clamp(t, 0f, 1f);
      return new Color((byte)(a.R + (b.R - a.R) * t), (byte)(a.G + (b.G - a.G) * t), (byte)(a.B + (b.B - a.B) * t));
    }

    private static Color ParseHex(string hex)
    {
      hex = hex.Replace("#", "");
      if (hex.Length != 6) return new Color(255, 255, 255);
      return new Color(Convert.ToByte(hex[..2], 16), Convert.ToByte(hex.Substring(2, 2), 16), Convert.ToByte(hex.Substring(4, 2), 16));
    }
  }
}