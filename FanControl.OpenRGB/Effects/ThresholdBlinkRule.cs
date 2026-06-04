using FanControl.OpenRGB.Rules;
using OpenRGB.NET;

namespace FanControl.OpenRGB.Effects
{
  // EFFECT 3: Event-driven rule that blinks a specific LED key if a threshold is crossed
  public class ThresholdBlinkRule(string deviceRegex, int ledIndex, float threshold, Color alertColor, Color idleColor) : BaseRgbRule(deviceRegex)
  {
    private readonly int _ledIndex = ledIndex;
    private readonly float _threshold = threshold;
    private readonly Color _alertColor = alertColor;
    private readonly Color _idleColor = idleColor;

    protected override void ProcessEffect(OpenRgbClient client, Device device, int deviceIndex, float value, int frameCount)
    {
      // Safeguard to avoid index out of bounds on the keyboard array
      if (_ledIndex >= device.Leds.Length) return;

      // Generate a global static color for the background (e.g. standard profile)
      Color[] colors = [.. Enumerable.Repeat(_idleColor, device.Leds.Length)];

      // Check if the source curve has crossed the alert threshold
      if (value > _threshold)
      {
        // Blink every 10 frames (~300ms)
        bool isOn = (frameCount % 10) < 5;
        colors[_ledIndex] = isOn ? _alertColor : new Color(0, 0, 0);
      }

      client.UpdateLeds(deviceIndex, colors);
    }
  }
}