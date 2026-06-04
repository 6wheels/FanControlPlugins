using FanControl.OpenRGB.Rules;
using OpenRGB.NET;

namespace FanControl.OpenRGB.Effects
{
  // EFFECT 3: Event-driven rule that blinks a specific LED key if a threshold is crossed
  public class ThresholdBlinkRule : BaseRgbRule
  {
    public string NormalColorHex { get; set; } = "#00FF00";
    public string BlinkColorHex { get; set; } = "#FF0000";
    public float Threshold { get; set; } = 80f; // S'active si charge > 80%
    public int BlinkIntervalFrames { get; set; } = 15; // Vitesse du clignotement

    protected override void ProcessEffect(OpenRgbClient client, Device device, int deviceIndex, string? zoneRegex, float value, int frameCount)
    {
      Color normal = ParseHex(NormalColorHex);
      Color blink = ParseHex(BlinkColorHex);

      // Détermine si on doit clignoter en fonction du seuil et du compteur de frames
      bool isBlinking = value >= Threshold && (frameCount % (BlinkIntervalFrames * 2) < BlinkIntervalFrames);

      Color targetColor = isBlinking ? blink : normal;
      Color[] colors = client.GetControllerData(deviceIndex).Colors;

      // On applique sans fondu (1.0f) pour un clignotement net
      ApplyToTargetLeds(device, zoneRegex, colors, targetColor, 1.0f);

      client.UpdateLeds(deviceIndex, colors);
    }

    private static Color ParseHex(string hex)
    {
      hex = hex.Replace("#", "");
      if (hex.Length != 6) return new Color(255, 255, 255);
      return new Color(Convert.ToByte(hex[..2], 16), Convert.ToByte(hex.Substring(2, 2), 16), Convert.ToByte(hex.Substring(4, 2), 16));
    }
  }
}