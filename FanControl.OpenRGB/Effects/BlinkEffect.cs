using OpenRGB.NET;
using System.Text.Json.Serialization;

namespace FanControl.OpenRGB.Effects
{
  public class BlinkEffect : BaseRgbEffect
  {
    public string Color1Hex { get; set; } = "#FF0000";
    public string Color2Hex { get; set; } = "#000000";

    private float _slowBlinkHz = 0.5f;
    private float _fastBlinkHz = 15.0f;

    public float SlowBlinkHz
    {
      get => _slowBlinkHz;
      set => _slowBlinkHz = Math.Max(0.1f, value);
    }

    public float FastBlinkHz
    {
      get => _fastBlinkHz;
      set => _fastBlinkHz = Math.Max(0.1f, value);
    }

    [JsonIgnore]
    public int Framerate { get; set; } = 30;

    protected override void ProcessEffect(Device device, string? zoneRegex, string? ledRegex, float value, int frameCount, float transitionSpeed, Color[] buffer)
    {
      Color c1 = ParseHex(Color1Hex);
      Color c2 = ParseHex(Color2Hex);

      float ratio = ModulateByValue ? Math.Clamp(value / 100f, 0.0f, 1.0f) : 1.0f;
      float currentHz = SlowBlinkHz + (FastBlinkHz - SlowBlinkHz) * ratio;
      float framesPerHalfPeriod = Math.Max(1f, Framerate / (2f * currentHz));
      float period = framesPerHalfPeriod * 2f;
      float phase = frameCount % period;
      bool isColor1 = phase < framesPerHalfPeriod;
      Color targetColor = isColor1 ? c1 : c2;

      ApplyToTargetLeds(device, zoneRegex, ledRegex, buffer, targetColor, 1.0f);
    }
  }
}
