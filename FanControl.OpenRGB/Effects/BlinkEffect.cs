using OpenRGB.NET;

namespace FanControl.OpenRGB.Effects
{
  public class BlinkEffect : BaseRgbEffect
  {
    public string Color1Hex { get; set; } = "#FF0000";
    public string Color2Hex { get; set; } = "#000000";

    private int _blinkIntervalFrames = 15;

    // Safeguard: We prevent going below 1 frame, otherwise division by zero or freeze
    public int BlinkIntervalFrames
    {
      get => _blinkIntervalFrames;
      set => _blinkIntervalFrames = Math.Max(1, value);
    }

    private int _maxBlinkIntervalFrames = 30;
    private int _minBlinkIntervalFrames = 2;
    public int MaxBlinkIntervalFrames
    {
      get => _maxBlinkIntervalFrames;
      set => _maxBlinkIntervalFrames = Math.Min(30, value);
    }
    public int MinBlinkIntervalFrames
    {
      get => _minBlinkIntervalFrames;
      set => _minBlinkIntervalFrames = Math.Max(1, value);
    }

    protected override void ProcessEffect(Device device, string? zoneRegex, string? ledRegex, float value, int frameCount, float transitionSpeed, Color[] buffer)
    {
      Color c1 = ParseHex(Color1Hex);
      Color c2 = ParseHex(Color2Hex);

      float ratio = ModulateByValue ? Math.Clamp(value / 100f, 0.0f, 1.0f) : 1.0f;

      // The blinking loops indefinitely while the effect is active.
      // Use a floating-point interval to reduce the jitter when the value changes dynamically.
      float currentInterval = MaxBlinkIntervalFrames - (MaxBlinkIntervalFrames - MinBlinkIntervalFrames) * ratio;
      currentInterval = Math.Max(1f, currentInterval);
      float period = currentInterval * 2f;
      float phase = frameCount % period;
      bool isColor1 = phase < currentInterval;
      Color targetColor = isColor1 ? c1 : c2;

      ApplyToTargetLeds(device, zoneRegex, ledRegex, buffer, targetColor, 1.0f); // No transition for blinking, we want an immediate switch
    }
  }
}