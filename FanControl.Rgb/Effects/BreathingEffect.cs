using OpenRGB.NET;

using FanControl.Rgb.Toolkit.Rendering;

namespace FanControl.Rgb.Effects
{
  public class BreathingEffect : BaseRgbEffect
  {
    public string BaseColorHex { get; set; } = "#000022";
    public string PeakColorHex { get; set; } = "#0000FF";

    public float MinSpeed { get; set; } = 1.0f; // relative speed at value=0, 0.0-10.0 (1.0 ≈ one breath / 4s)
    public float MaxSpeed { get; set; } = 3.0f; // relative speed at value=100, 0.0-10.0

    protected override void ProcessEffect(IRgbDevice device, string? zoneRegex, string? ledRegex, float value, int frameCount, int framerate, float transitionSpeed, Color[] buffer)
    {
      Color baseCol = ParseHex(BaseColorHex);
      Color peakCol = ParseHex(PeakColorHex);

      float ratio = ModulateByValue ? Math.Clamp(value / 100f, 0.0f, 1.0f) : 1.0f;
      // Min/MaxSpeed are relative multipliers of the shared reference rate; convert the
      // frame index to seconds so the breathing rate stays framerate-independent.
      float currentHz = (MinSpeed + ratio * (MaxSpeed - MinSpeed)) * ReferenceCyclesPerSecond;
      float seconds = framerate > 0 ? frameCount / (float)framerate : 0f;

      double sine = (Math.Sin(seconds * currentHz * 2.0 * Math.PI) + 1.0) / 2.0;

      byte r = (byte)(baseCol.R + (peakCol.R - baseCol.R) * sine);
      byte g = (byte)(baseCol.G + (peakCol.G - baseCol.G) * sine);
      byte b = (byte)(baseCol.B + (peakCol.B - baseCol.B) * sine);

      Color target = new(r, g, b);
      float fade = NormalizeFade(transitionSpeed, framerate);
      ApplyToTargetLeds(device, zoneRegex, ledRegex, buffer, target, fade);
    }
  }
}