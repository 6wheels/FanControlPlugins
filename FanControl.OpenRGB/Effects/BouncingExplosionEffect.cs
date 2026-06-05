using System.Text.RegularExpressions;
using OpenRGB.NET;

namespace FanControl.OpenRGB.Effects
{

  // EFFECT 5: The "Crazy" Bouncing Ball & Confetti Explosion
  // 1. The original class (Make sure it inherits from BaseRgbRule)
  public class BouncingExplosionEffect : BaseRgbEffect
  {
    public string BgColorHex { get; set; } = "#000000";
    public string DotColorHex { get; set; } = "#FF00FF";
    public int SpeedDivisor { get; set; } = 2; // The larger the number, the slower it is

    protected override void ProcessEffect(OpenRgbClient client, Device device, int deviceIndex, string? zoneRegex, float value, int frameCount)
    {
      Color bg = ParseHex(BgColorHex);
      Color dot = ParseHex(DotColorHex);
      Color[] colors = client.GetControllerData(deviceIndex).Colors;

      int ledOffset = 0;
      foreach (var zone in device.Zones)
      {
        if ((string.IsNullOrEmpty(zoneRegex) || Regex.IsMatch(zone.Name, zoneRegex)) && zone.LedCount > 0)
        {
          // Mathematical formula to calculate the index of the back-and-forth point
          int cycleLength = (int)zone.LedCount * 2 - 2;
          if (cycleLength <= 0) cycleLength = 1;

          int step = frameCount / SpeedDivisor % cycleLength;
          int dotIndex = step < zone.LedCount ? step : cycleLength - step;

          for (int l = 0; l < zone.LedCount; l++)
          {
            colors[ledOffset + l] = (l == dotIndex) ? dot : bg;
          }
        }
        ledOffset += (int)zone.LedCount;
      }

      client.UpdateLeds(deviceIndex, colors);
    }

    private static Color ParseHex(string hex)
    { /* Same */
      hex = hex.Replace("#", "");
      if (hex.Length != 6) return new Color(255, 255, 255);
      return new Color(Convert.ToByte(hex[..2], 16), Convert.ToByte(hex.Substring(2, 2), 16), Convert.ToByte(hex.Substring(4, 2), 16));
    }
  }
}