using OpenRGB.NET;

namespace FanControl.OpenRGB.Effects
{
  public class GradientEffect : BaseRgbEffect
  {
    public string ColorMinHex { get; set; } = "#00FF00";
    public string ColorMaxHex { get; set; } = "#FF0000";

    protected override void ProcessEffect(Device device, string? zoneRegex, string? ledRegex, float value, int frameCount, float transitionSpeed, Color[] buffer)
    {
      Color colorMin = ParseHex(ColorMinHex);
      Color colorMax = ParseHex(ColorMaxHex);

      float ratio = Math.Clamp(value / 100f, 0.0f, 1.0f);

      byte r = (byte)(colorMin.R + (colorMax.R - colorMin.R) * ratio);
      byte g = (byte)(colorMin.G + (colorMax.G - colorMin.G) * ratio);
      byte b = (byte)(colorMin.B + (colorMax.B - colorMin.B) * ratio);

      ApplyToTargetLeds(device, zoneRegex, ledRegex, buffer, new Color(r, g, b), transitionSpeed);
    }
  }
}