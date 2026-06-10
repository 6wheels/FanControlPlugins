using OpenRGB.NET;

namespace FanControl.OpenRGB.Effects
{
  public class StaticEffect : BaseRgbEffect
  {
    public string ColorHex { get; set; } = "#FFFFFF";

    protected override void ProcessEffect(Device device, string? zoneRegex, string? ledRegex, float value, int frameCount, float transitionSpeed, Color[] buffer)
    {
      Color baseColor = ParseHex(ColorHex);
      float intensity = ModulateByValue ? Math.Clamp(value / 100f, 0.0f, 1.0f) : 1.0f;
      Color targetColor = new(
                     (byte)(baseColor.R * intensity),
                     (byte)(baseColor.G * intensity),
                     (byte)(baseColor.B * intensity)
                 );

      ApplyToTargetLeds(device, zoneRegex, ledRegex, buffer, targetColor, transitionSpeed);
    }
  }
}