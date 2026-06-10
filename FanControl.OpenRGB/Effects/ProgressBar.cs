using System;
using System.Collections.Generic;
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

      // Convert the current value into a normalized fill ratio between 0.0 and 1.0.
      float fillRatio = Math.Clamp(value / 100f, 0f, 1f);

      int ledOffset = 0;
      foreach (var zone in device.Zones)
      {
        if (string.IsNullOrEmpty(zoneRegex) || Regex.IsMatch(zone.Name, zoneRegex))
        {
          var targetLeds = new List<(int GlobalIndex, int Row, int Column)>();

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
                  int globalIndex = ledOffset + (int)ledIndex;
                  string ledName = device.Leds[globalIndex].Name;
                  if (string.IsNullOrEmpty(ledRegex) || Regex.IsMatch(ledName, ledRegex))
                  {
                    targetLeds.Add((globalIndex, y, x));
                  }
                }
              }
            }
          }
          else
          {
            for (int l = 0; l < zone.LedCount; l++)
            {
              int globalIndex = ledOffset + l;
              string ledName = device.Leds[globalIndex].Name;
              if (string.IsNullOrEmpty(ledRegex) || Regex.IsMatch(ledName, ledRegex))
              {
                targetLeds.Add((globalIndex, 0, l));
              }
            }
          }

          if (targetLeds.Count > 0)
          {
            // Ensures consistent top-to-bottom, left-to-right fill order across 1D and 2D zones.
            targetLeds.Sort((a, b) =>
            {
              int rowCompare = a.Row.CompareTo(b.Row);
              return rowCompare != 0 ? rowCompare : a.Column.CompareTo(b.Column);
            });

            int targetCount = targetLeds.Count;
            int fillCount = (int)Math.Round(fillRatio * targetCount);
            fillCount = Math.Clamp(fillCount, 0, targetCount);
            // Guard: float rounding can leave the last LED unfilled at exactly 100%.
            if (fillRatio >= 0.9999f) fillCount = targetCount;

            for (int i = 0; i < targetCount; i++)
            {
              int ledIndex = targetLeds[i].GlobalIndex;
              if (i < fillCount)
              {
                buffer[ledIndex] = LerpColor(buffer[ledIndex], fillCol, transitionSpeed);
              }
              else if (!isTransparent)
              {
                buffer[ledIndex] = LerpColor(buffer[ledIndex], emptyCol, transitionSpeed);
              }
            }
          }
        }
        ledOffset += (int)zone.LedCount;
      }
    }
  }
}