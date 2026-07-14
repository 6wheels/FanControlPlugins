using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using OpenRGB.NET;

using FanControl.Rgb.Toolkit.Rendering;

namespace FanControl.Rgb.Effects
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

    private float _speed = 1.0f;
    public float Speed
    {
      // Relative wave speed; 1.0 ≈ one cycle every 4s. Clamped very slow to very fast.
      get => _speed;
      set => _speed = Math.Clamp(value, 0.001f, 8.0f); // 0.001-8.0
    }

    private float _scale = 0.3f;
    public float Scale
    {
      // Spatial frequency of the wave across LEDs. A scale of 0 would crush the wave.
      get => _scale;
      set => _scale = Math.Clamp(value, 0.01f, 5.0f); // 0.01-5.0
    }

    private float _saturation = 1.0f;
    public float Saturation
    {
      // Desaturates the wave palette without touching the anchor hex colors.
      get => _saturation;
      set => _saturation = Math.Clamp(value, 0.0f, 1.0f); // 0.0-1.0
    }

    public AuroraDirection Direction { get; set; } = AuroraDirection.Horizontal;

    protected override void ProcessEffect(IRgbDevice device, string? zoneRegex, string? ledRegex, float value, int frameCount, int framerate, float transitionSpeed, LedWriter writer)
    {
      Color c1 = ParseHex(Color1Hex);
      Color c2 = ParseHex(Color2Hex);
      Color c3 = ParseHex(Color3Hex);

      // Convert frame index to seconds so Speed is cycles/sec regardless of framerate.
      float seconds = framerate > 0 ? frameCount / (float)framerate : 0f;
      float time = seconds * Speed * ReferenceCyclesPerSecond * 2f * (float)Math.PI;
      bool isVertical = Direction == AuroraDirection.Vertical;

      float intensity = ModulateByValue ? Math.Clamp(value / 100f, 0.0f, 1.0f) : 1.0f;
      float fade = NormalizeFade(transitionSpeed, framerate);

      int ledOffset = 0;
      foreach (var zone in device.Zones)
      {
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
                if (ledIndex != 0xFFFFFFFF && ledOffset + ledIndex < writer.Length)
                {
                  string ledName = device.Leds[ledOffset + (int)ledIndex].Name;

                  if (string.IsNullOrEmpty(ledRegex) || Regex.IsMatch(ledName, ledRegex))
                  {
                    Color waveColor = CalculateAuroraColor(x, y, time, c1, c2, c3, isVertical);

                    Color targetColor = new Color(
                        (byte)(waveColor.R * intensity),
                        (byte)(waveColor.G * intensity),
                        (byte)(waveColor.B * intensity)
                    );

                    writer.Write((int)(ledOffset + ledIndex), targetColor, fade);
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

                writer.Write(ledOffset + l, targetColor, fade);
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

      // Two offset sine waves on perpendicular axes create an organic interference pattern.
      // The 0.7x speed difference on the secondary wave prevents a static diagonal grid.
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

      // Sum of two [-1,1] waves → [-2,2]; normalize to [0,1].
      float factor = (float)((mainWave + secondaryWave + 2.0) / 4.0);

      // Split the 3-color ramp at the midpoint: c1→c2 in the lower half, c2→c3 in the upper half.
      Color ramp = factor < 0.5f
          ? LerpColor(c1, c2, factor * 2f)
          : LerpColor(c2, c3, (factor - 0.5f) * 2f);

      return AdjustSaturation(ramp, Saturation);
    }
  }
}