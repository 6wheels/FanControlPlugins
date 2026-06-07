using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using OpenRGB.NET;

namespace FanControl.OpenRGB.Effects
{
  public class GaugeGradientEffect : BaseRgbEffect
  {
    public string ColorMinHex { get; set; } = "#00FF00";
    public string ColorMaxHex { get; set; } = "#FF0000";

    protected override void ProcessEffect(Device device, string? zoneRegex, string? ledRegex, float value, int frameCount, float transitionSpeed, Color[] buffer)
    {
      Color baseMin = ParseHex(ColorMinHex);
      Color baseMax = ParseHex(ColorMaxHex);

      float valueRatio = ModulateByValue ? Math.Clamp(value / 100f, 0.0f, 1.0f) : 1.0f;
      int ledOffset = 0;

      foreach (var zone in device.Zones)
      {
        if (string.IsNullOrEmpty(zoneRegex) || Regex.IsMatch(zone.Name, zoneRegex))
        {
          var targetLeds = new List<int>();

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
                    targetLeds.Add(ledOffset + (int)ledIndex);
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
                targetLeds.Add(globalIndex);
              }
            }
          }

          if (targetLeds.Count > 0)
          {
            float fillPosition = valueRatio * targetLeds.Count;
            int filledCount = Math.Clamp((int)Math.Floor(fillPosition), 0, targetLeds.Count);
            float edgeWidth = Math.Clamp(targetLeds.Count / 4f, 2f, 5f);

            for (int i = 0; i < targetLeds.Count; i++)
            {
              float weight;
              if (i < filledCount)
              {
                weight = 1f;
              }
              else
              {
                float distance = i - fillPosition + 0.5f;
                weight = Math.Clamp(1f - distance / edgeWidth, 0f, 1f);
              }

              int ledIndex = targetLeds[i];
              Color targetColor = LerpColor(baseMin, baseMax, weight);
              buffer[ledIndex] = LerpColor(buffer[ledIndex], targetColor, transitionSpeed);
            }
          }
        }
        ledOffset += (int)zone.LedCount;
      }
    }

  }
}
