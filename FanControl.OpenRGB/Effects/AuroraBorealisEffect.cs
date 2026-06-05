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

  public class AuroraBorealisEffect : BaseRgbEffect
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

    // NEW: Choice of wave direction ("Horizontal" or "Vertical")
    public AuroraDirection Direction { get; set; } = AuroraDirection.Horizontal;

    protected override void ProcessEffect(OpenRgbClient client, Device device, int deviceIndex, string? zoneRegex, float value, int frameCount)
    {
      Color c1 = ParseHex(Color1Hex);
      Color c2 = ParseHex(Color2Hex);
      Color c3 = ParseHex(Color3Hex);

      Color[] colors = client.GetControllerData(deviceIndex).Colors;
      float time = frameCount * Speed;
      bool isVertical = Direction == AuroraDirection.Vertical;

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
                if (ledIndex != 0xFFFFFFFF && ledOffset + ledIndex < colors.Length)
                {
                  // We pass isVertical to the calculation method
                  colors[ledOffset + ledIndex] = CalculateAuroraColor(x, y, time, c1, c2, c3, isVertical);
                }
              }
            }
          }
          else
          {
            for (int l = 0; l < zone.LedCount; l++)
            {
              colors[ledOffset + l] = CalculateAuroraColor(l, 0, time, c1, c2, c3, isVertical);
            }
          }
        }
        ledOffset += (int)zone.LedCount;
      }
      client.UpdateLeds(deviceIndex, colors);
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