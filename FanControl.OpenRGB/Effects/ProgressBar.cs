using System;
using System.Text.RegularExpressions;
using OpenRGB.NET;

namespace FanControl.OpenRGB.Effects
{
  public class ProgressBarEffect : BaseRgbEffect
  {
    public string FillColorHex { get; set; } = "#FF0000";
    public string EmptyColorHex { get; set; } = "Transparent"; // "Transparent" or Hexadecimal

    protected override void ProcessEffect(Device device, string? zoneRegex, string? ledRegex, float value, int frameCount, float transitionSpeed, Color[] buffer)
    {
      Color fillCol = ParseHex(FillColorHex);
      bool isTransparent = string.IsNullOrEmpty(EmptyColorHex) || EmptyColorHex.Equals("Transparent", StringComparison.OrdinalIgnoreCase);
      Color emptyCol = isTransparent ? new Color() : ParseHex(EmptyColorHex);

      // Interpolation déjà calculée de 0.0 à 1.0 par rapport au seuil
      float fillRatio = Math.Clamp(value / 100f, 0f, 1f);

      int ledOffset = 0;
      foreach (var zone in device.Zones)
      {
        if (string.IsNullOrEmpty(zoneRegex) || Regex.IsMatch(zone.Name, zoneRegex))
        {
          // === MODE 2D (Matrice) ===
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
                    float ledRatio = width > 1 ? (float)x / (width - 1) : 0f;

                    if (ledRatio <= fillRatio)
                    {
                      buffer[ledOffset + ledIndex] = LerpColor(buffer[ledOffset + ledIndex], fillCol, transitionSpeed);
                    }
                    else if (!isTransparent)
                    {
                      buffer[ledOffset + ledIndex] = LerpColor(buffer[ledOffset + ledIndex], emptyCol, transitionSpeed);
                    }
                  }
                }
              }
            }
          }
          // === MODE 1D ===
          else
          {
            for (int l = 0; l < zone.LedCount; l++)
            {
              string ledName = device.Leds[ledOffset + l].Name;
              if (string.IsNullOrEmpty(ledRegex) || Regex.IsMatch(ledName, ledRegex))
              {
                float ledRatio = zone.LedCount > 1 ? (float)l / (zone.LedCount - 1) : 0f;

                if (ledRatio <= fillRatio)
                {
                  buffer[ledOffset + l] = LerpColor(buffer[ledOffset + l], fillCol, transitionSpeed);
                }
                else if (!isTransparent)
                {
                  buffer[ledOffset + l] = LerpColor(buffer[ledOffset + l], emptyCol, transitionSpeed);
                }
              }
            }
          }
        }
        ledOffset += (int)zone.LedCount;
      }
    }
  }
}