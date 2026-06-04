using FanControl.OpenRGB.Rules;
using OpenRGB.NET;

namespace FanControl.OpenRGB.Effects
{
  public class ThermalGradientRule : BaseRgbRule
  {
    // Propriétés publiques pour le JSON
    public string ColorMinHex { get; set; } = "#00FF00"; // Vert par défaut
    public string ColorMaxHex { get; set; } = "#FF0000"; // Rouge par défaut
    public float FadeSpeed { get; set; } = 0.1f;

    protected override void ProcessEffect(OpenRgbClient client, Device device, int deviceIndex, string? zoneRegex, float value, int frameCount)
    {
      Color colorMin = ParseHex(ColorMinHex);
      Color colorMax = ParseHex(ColorMaxHex);

      // Calcul du ratio de température (0.0 à 1.0)
      float ratio = Math.Clamp(value / 100f, 0.0f, 1.0f);

      // Calcul de la couleur cible
      byte r = (byte)(colorMin.R + (colorMax.R - colorMin.R) * ratio);
      byte g = (byte)(colorMin.G + (colorMax.G - colorMin.G) * ratio);
      byte b = (byte)(colorMin.B + (colorMax.B - colorMin.B) * ratio);

      Color targetColor = new(r, g, b);

      Color[] colors = client.GetControllerData(deviceIndex).Colors;

      // Application avec le fondu
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