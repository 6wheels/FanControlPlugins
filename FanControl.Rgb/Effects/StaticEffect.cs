using OpenRGB.NET;

using FanControl.Rgb.Toolkit.Rendering;

namespace FanControl.Rgb.Effects
{
  public class StaticEffect : BaseRgbEffect
  {
    public string ColorHex { get; set; } = "#FFFFFF"; // hex RGB, "#RRGGBB"

    protected override void ProcessEffect(IRgbDevice device, string? zoneRegex, string? ledRegex, float value, int frameCount, int framerate, float transitionSpeed, Color[] buffer)
    {
      Color baseColor = ParseHex(ColorHex);
      float intensity = ModulateByValue ? Math.Clamp(value / 100f, 0.0f, 1.0f) : 1.0f;
      Color targetColor = new(
                     (byte)(baseColor.R * intensity),
                     (byte)(baseColor.G * intensity),
                     (byte)(baseColor.B * intensity)
                 );

      float fade = NormalizeFade(transitionSpeed, framerate);
      ApplyToTargetLeds(device, zoneRegex, ledRegex, buffer, targetColor, fade);
    }
  }
}