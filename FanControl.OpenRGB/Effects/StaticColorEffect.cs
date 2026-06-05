using OpenRGB.NET;

namespace FanControl.OpenRGB.Effects
{
  public class StaticColorEffect : BaseRgbEffect
  {
    public string ColorHex { get; set; } = "#FFFFFF";
    public float FadeSpeed { get; set; } = 0.05f;

    // L'override a maintenant exactement les 6 mêmes paramètres que la base
    protected override void ProcessEffect(OpenRgbClient client, Device device, int deviceIndex, string? zoneRegex, float value, int frameCount)
    {
      Color targetColor = ParseHex(ColorHex);
      Color[] colors = client.GetControllerData(deviceIndex).Colors;

      // L'appel prend bien ses 5 paramètres
      ApplyToTargetLeds(device, zoneRegex, colors, targetColor, FadeSpeed);

      client.UpdateLeds(deviceIndex, colors);
    }

    private static Color ParseHex(string hex)
    {
      hex = hex.Replace("#", "");
      if (hex.Length != 6) return new Color(255, 255, 255);
      return new Color(
          Convert.ToByte(hex.Substring(0, 2), 16),
          Convert.ToByte(hex.Substring(2, 2), 16),
          Convert.ToByte(hex.Substring(4, 2), 16)
      );
    }
  }
}