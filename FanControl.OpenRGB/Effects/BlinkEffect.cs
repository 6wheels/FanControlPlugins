using OpenRGB.NET;

namespace FanControl.OpenRGB.Effects
{
  public class BlinkEffect : BaseRgbEffect
  {
    public string Color1Hex { get; set; } = "#FF0000";
    public string Color2Hex { get; set; } = "#000000";

    private int _blinkIntervalFrames = 15;

    // Garde-fou : On empêche de descendre en dessous de 1 frame, sinon division par zéro ou freeze
    public int BlinkIntervalFrames
    {
      get => _blinkIntervalFrames;
      set => _blinkIntervalFrames = Math.Max(1, value);
    }

    protected override void ProcessEffect(OpenRgbClient client, Device device, int deviceIndex, string? zoneRegex, float value, int frameCount)
    {
      Color c1 = ParseHex(Color1Hex);
      Color c2 = ParseHex(Color2Hex);

      // Le clignotement tourne en boucle indéfiniment tant que l'effet est actif
      bool isColor1 = (frameCount % (BlinkIntervalFrames * 2)) < BlinkIntervalFrames;

      Color targetColor = isColor1 ? c1 : c2;
      Color[] colors = client.GetControllerData(deviceIndex).Colors;

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