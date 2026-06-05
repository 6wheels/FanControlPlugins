using System.Text.RegularExpressions;
using OpenRGB.NET;

namespace FanControl.OpenRGB.Effects
{
  // EFFECT 4: Linear filling gauge (Progress bar)
  public class ProgressBarEffect : BaseRgbEffect
  {
    public string EmptyColorHex { get; set; } = "#000000";
    public string FillColorHex { get; set; } = "#00FF00";

    protected override void ProcessEffect(OpenRgbClient client, Device device, int deviceIndex, string? zoneRegex, float value, int frameCount)
    {
      Color empty = ParseHex(EmptyColorHex);
      Color fill = ParseHex(FillColorHex);
      Color[] colors = client.GetControllerData(deviceIndex).Colors;

      float ratio = Math.Clamp(value / 100f, 0f, 1f);
      int ledOffset = 0;

      foreach (var zone in device.Zones)
      {
        // We check if the zone matches our targeting
        if (string.IsNullOrEmpty(zoneRegex) || Regex.IsMatch(zone.Name, zoneRegex))
        {
          int fillCount = (int)(ratio * zone.LedCount);
          for (int l = 0; l < zone.LedCount; l++)
          {
            colors[ledOffset + l] = (l < fillCount) ? fill : empty;
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