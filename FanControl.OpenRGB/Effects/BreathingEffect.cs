using OpenRGB.NET;

namespace FanControl.OpenRGB.Effects
{
  // EFFECT 2: Sinusoidal breathing where speed or baseline depends on the curve
  public class BreathingEffect : BaseRgbEffect
  {
    public string BaseColorHex { get; set; } = "#000022";
    public string PeakColorHex { get; set; } = "#0000FF";

    public float MinSpeed { get; set; } = 0.02f;
    public float MaxSpeed { get; set; } = 0.15f;

    protected override void ProcessEffect(OpenRgbClient client, Device device, int deviceIndex, string? zoneRegex, string? ledRegex, float value, int frameCount, float transitionSpeed, Color[] buffer)
    {
      Color baseCol = ParseHex(BaseColorHex);
      Color peakCol = ParseHex(PeakColorHex);

      // The higher the value (load), the faster it breathes
      float ratio = ModulateByValue ? Math.Clamp(value / 100f, 0.0f, 1.0f) : 1.0f;
      float currentSpeed = MinSpeed + ratio * (MaxSpeed - MinSpeed);

      // Calculation of a sinusoidal wave (between 0.0 and 1.0)
      double sine = (Math.Sin(frameCount * currentSpeed) + 1.0) / 2.0;

      byte r = (byte)(baseCol.R + (peakCol.R - baseCol.R) * sine);
      byte g = (byte)(baseCol.G + (peakCol.G - baseCol.G) * sine);
      byte b = (byte)(baseCol.B + (peakCol.B - baseCol.B) * sine);

      Color target = new(r, g, b);
      Color[] colors = client.GetControllerData(deviceIndex).Colors;

      ApplyToTargetLeds(device, zoneRegex, ledRegex, buffer, target, transitionSpeed);
    }

    private static Color ParseHex(string hex)
    { /* Same as above */
      hex = hex.Replace("#", "");
      if (hex.Length != 6) return new Color(255, 255, 255);
      return new Color(Convert.ToByte(hex[..2], 16), Convert.ToByte(hex.Substring(2, 2), 16), Convert.ToByte(hex.Substring(4, 2), 16));
    }
  }
}