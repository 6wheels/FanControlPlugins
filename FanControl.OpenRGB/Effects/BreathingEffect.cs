using OpenRGB.NET;

namespace FanControl.OpenRGB.Effects
{
  public class BreathingEffect : BaseRgbEffect
  {
    public string BaseColorHex { get; set; } = "#000022";
    public string PeakColorHex { get; set; } = "#0000FF";

    public float MinSpeed { get; set; } = 0.02f;
    public float MaxSpeed { get; set; } = 0.15f;

    protected override void ProcessEffect(Device device, string? zoneRegex, string? ledRegex, float value, int frameCount, float transitionSpeed, Color[] buffer)
    {
      Color baseCol = ParseHex(BaseColorHex);
      Color peakCol = ParseHex(PeakColorHex);

      float ratio = ModulateByValue ? Math.Clamp(value / 100f, 0.0f, 1.0f) : 1.0f;
      float currentSpeed = MinSpeed + ratio * (MaxSpeed - MinSpeed);

      double sine = (Math.Sin(frameCount * currentSpeed) + 1.0) / 2.0;

      byte r = (byte)(baseCol.R + (peakCol.R - baseCol.R) * sine);
      byte g = (byte)(baseCol.G + (peakCol.G - baseCol.G) * sine);
      byte b = (byte)(baseCol.B + (peakCol.B - baseCol.B) * sine);

      Color target = new(r, g, b);
      ApplyToTargetLeds(device, zoneRegex, ledRegex, buffer, target, transitionSpeed);
    }
  }
}