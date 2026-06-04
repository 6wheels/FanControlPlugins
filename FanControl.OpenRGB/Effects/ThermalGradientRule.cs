using FanControl.OpenRGB.Rules;
using OpenRGB.NET;

namespace FanControl.OpenRGB.Effects
{
  // EFFECT 1: Linear interpolation between two colors based on percentage
  public class ThermalGradientRule(string deviceRegex, Color colorMin, Color colorMax) : BaseRgbRule(deviceRegex)
  {
    private readonly Color _colorMin = colorMin;
    private readonly Color _colorMax = colorMax;

    protected override void ProcessEffect(OpenRgbClient client, Device device, int deviceIndex, float value, int frameCount)
    {
      float ratio = Math.Clamp(value / 100f, 0.0f, 1.0f);

      byte r = (byte)(_colorMin.R + (_colorMax.R - _colorMin.R) * ratio);
      byte g = (byte)(_colorMin.G + (_colorMax.G - _colorMin.G) * ratio);
      byte b = (byte)(_colorMin.B + (_colorMax.B - _colorMin.B) * ratio);

      Color targetColor = new(r, g, b);

      Color[] colors = [.. Enumerable.Repeat(targetColor, device.Leds.Length)];
      client.UpdateLeds(deviceIndex, colors);
    }
  }
}