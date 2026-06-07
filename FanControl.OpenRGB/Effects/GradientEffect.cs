using OpenRGB.NET;

namespace FanControl.OpenRGB.Effects
{
  public class GradientEffect : BaseRgbEffect
  {
    // Public properties for JSON
    public string ColorMinHex { get; set; } = "#00FF00"; // Green by default
    public string ColorMaxHex { get; set; } = "#FF0000"; // Red by default

    protected override void ProcessEffect(Device device, string? zoneRegex, string? ledRegex, float value, int frameCount, float transitionSpeed, Color[] buffer)
    {
      Color colorMin = ParseHex(ColorMinHex);
      Color colorMax = ParseHex(ColorMaxHex);

      // Calculation of temperature ratio (0.0 to 1.0)
      float ratio = Math.Clamp(value / 100f, 0.0f, 1.0f);

      // Calculation of target color
      byte r = (byte)(colorMin.R + (colorMax.R - colorMin.R) * ratio);
      byte g = (byte)(colorMin.G + (colorMax.G - colorMin.G) * ratio);
      byte b = (byte)(colorMin.B + (colorMax.B - colorMin.B) * ratio);

      Color targetColor = new(r, g, b);

      // Application with fade
      ApplyToTargetLeds(device, zoneRegex, ledRegex, buffer, targetColor, transitionSpeed);
    }
  }
}