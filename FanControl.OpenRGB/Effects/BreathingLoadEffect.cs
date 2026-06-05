using OpenRGB.NET;

namespace FanControl.OpenRGB.Effects
{
  // EFFECT 2: Sinusoidal breathing where speed or baseline depends on the curve
  public class BreathingLoadEffect : BaseRgbEffect
  {
    public string BaseColorHex { get; set; } = "#000022";
    public string PeakColorHex { get; set; } = "#0000FF";
    public float BaseSpeed { get; set; } = 0.05f;

    protected override void ProcessEffect(OpenRgbClient client, Device device, int deviceIndex, string? zoneRegex, float value, int frameCount)
    {
      Color baseCol = ParseHex(BaseColorHex);
      Color peakCol = ParseHex(PeakColorHex);

      // Plus la valeur (load) est haute, plus ça respire vite
      float currentSpeed = BaseSpeed + (Math.Clamp(value, 0f, 100f) / 100f) * 0.15f;

      // Calcul d'une onde sinusoïdale (entre 0.0 et 1.0)
      double sine = (Math.Sin(frameCount * currentSpeed) + 1.0) / 2.0;

      byte r = (byte)(baseCol.R + (peakCol.R - baseCol.R) * sine);
      byte g = (byte)(baseCol.G + (peakCol.G - baseCol.G) * sine);
      byte b = (byte)(baseCol.B + (peakCol.B - baseCol.B) * sine);

      Color target = new(r, g, b);
      Color[] colors = client.GetControllerData(deviceIndex).Colors;

      ApplyToTargetLeds(device, zoneRegex, colors, target, 1.0f);
      client.UpdateLeds(deviceIndex, colors);
    }

    private static Color ParseHex(string hex)
    { /* Pareil que ci-dessus */
      hex = hex.Replace("#", "");
      if (hex.Length != 6) return new Color(255, 255, 255);
      return new Color(Convert.ToByte(hex[..2], 16), Convert.ToByte(hex.Substring(2, 2), 16), Convert.ToByte(hex.Substring(4, 2), 16));
    }
  }
}