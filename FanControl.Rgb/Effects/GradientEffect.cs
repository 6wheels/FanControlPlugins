using OpenRGB.NET;

using FanControl.Rgb.Toolkit.Rendering;

namespace FanControl.Rgb.Effects
{
  public class GradientEffect : BaseRgbEffect
  {
    // 2-stop fallback used when ColorStops is empty.
    public string ColorMinHex { get; set; } = "#00FF00";
    public string ColorMaxHex { get; set; } = "#FF0000";

    // Optional multi-stop gradient. When non-empty it overrides ColorMinHex/ColorMaxHex.
    // Stop positions are 0.0-1.0; the sensor ratio (value/100) selects the color.
    public List<GradientStop> ColorStops { get; set; } = new();

    protected override void ProcessEffect(IRgbDevice device, string? zoneRegex, string? ledRegex, float value, int frameCount, int framerate, float transitionSpeed, Color[] buffer)
    {
      var stops = ResolveStops(ColorStops, ColorMinHex, ColorMaxHex);

      float ratio = Math.Clamp(value / 100f, 0.0f, 1.0f);
      Color target = SampleGradient(stops, ratio);

      float fade = NormalizeFade(transitionSpeed, framerate);
      ApplyToTargetLeds(device, zoneRegex, ledRegex, buffer, target, fade);
    }
  }
}