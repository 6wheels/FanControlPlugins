using OpenRGB.NET;

using FanControl.Rgb.Toolkit.Rendering;

namespace FanControl.Rgb.Effects
{
  public class BlinkEffect : BaseRgbEffect
  {
    public string Color1Hex { get; set; } = "#FF0000";
    public string Color2Hex { get; set; } = "#000000";

    private float _slowBlinkHz = 0.5f;
    private float _fastBlinkHz = 5.0f;

    // Blink rate at value=0. blinks/sec, >= 0.1.
    public float SlowBlinkHz
    {
      get => _slowBlinkHz;
      set => _slowBlinkHz = Math.Max(0.1f, value);
    }

    // Blink rate at value=100. blinks/sec, >= 0.1.
    public float FastBlinkHz
    {
      get => _fastBlinkHz;
      set => _fastBlinkHz = Math.Max(0.1f, value);
    }

    protected override void ProcessEffect(IRgbDevice device, string? zoneRegex, string? ledRegex, float value, int frameCount, int framerate, float transitionSpeed, Color[] buffer)
    {
      Color c1 = ParseHex(Color1Hex);
      Color c2 = ParseHex(Color2Hex);

      float ratio = ModulateByValue ? Math.Clamp(value / 100f, 0.0f, 1.0f) : 1.0f;
      float currentHz = SlowBlinkHz + (FastBlinkHz - SlowBlinkHz) * ratio;
      // Use the sink's real framerate so the Hz rate holds regardless of frame cadence.
      float effectiveRate = framerate > 0 ? framerate : 30;
      float framesPerHalfPeriod = Math.Max(1f, effectiveRate / (2f * currentHz));
      float period = framesPerHalfPeriod * 2f;
      float phase = frameCount % period;
      bool isColor1 = phase < framesPerHalfPeriod;
      Color targetColor = isColor1 ? c1 : c2;

      ApplyToTargetLeds(device, zoneRegex, ledRegex, buffer, targetColor, 1.0f);
    }
  }
}
