using FanControl.OpenRGB.Rules;
using OpenRGB.NET;

namespace FanControl.OpenRGB.Effects
{
  // EFFECT 2: Sinusoidal breathing where speed or baseline depends on the curve
  public class BreathingLoadRule(string deviceRegex, Color baseColor) : BaseRgbRule(deviceRegex)
  {
    private readonly Color _baseColor = baseColor;

    protected override void ProcessEffect(OpenRgbClient client, Device device, int deviceIndex, float value, int frameCount)
    {
      // Speed up the wave oscillation if the load (%) is high
      float speedModifier = (value > 70f) ? 0.12f : 0.05f;
      float pulse = (float)(Math.Sin(frameCount * speedModifier) + 1.0) / 2.0f;

      Color targetColor = new(
          (byte)(_baseColor.R * pulse),
          (byte)(_baseColor.G * pulse),
          (byte)(_baseColor.B * pulse)
      );

      Color[] colors = [.. Enumerable.Repeat(targetColor, device.Leds.Length)];
      client.UpdateLeds(deviceIndex, colors);
    }
  }
}