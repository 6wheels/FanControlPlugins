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

    protected override void ProcessEffect(OpenRgbClient client, Device device, int deviceIndex, string? zoneRegex, string? ledRegex, float value, int frameCount, float transitionSpeed)
    {
      Color c1 = ParseHex(Color1Hex);
      Color c2 = ParseHex(Color2Hex);

      float ratio = Math.Clamp(value / 100f, 0.0f, 1.0f);

      // The blinking loops indefinitely while the effect is active
      int currentInterval = (int)(MaxBlinkIntervalFrames - (MaxBlinkIntervalFrames - MinBlinkIntervalFrames) * ratio);
      currentInterval = Math.Max(1, currentInterval);

      bool isColor1 = (frameCount % (currentInterval * 2)) < currentInterval;
      Color targetColor = isColor1 ? c1 : c2;

      Color[] colors = client.GetControllerData(deviceIndex).Colors;
      ApplyToTargetLeds(device, zoneRegex, ledRegex, colors, targetColor, transitionSpeed);
      client.UpdateLeds(deviceIndex, colors);
    }

    private static Color ParseHex(string hex)
    {
      hex = hex.Replace("#", "");
      if (hex.Length != 6) return new Color(255, 255, 255);
      return new Color(Convert.ToByte(hex[..2], 16), Convert.ToByte(hex.Substring(2, 2), 16), Convert.ToByte(hex.Substring(4, 2), 16));
    }
  }
}