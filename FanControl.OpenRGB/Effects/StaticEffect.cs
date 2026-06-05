using OpenRGB.NET;

namespace FanControl.OpenRGB.Effects
{
  public class StaticEffect : BaseRgbEffect
  {
    public string ColorHex { get; set; } = "#FFFFFF";

    // The override now has exactly the same 6 parameters as the base
    protected override void ProcessEffect(OpenRgbClient client, Device device, int deviceIndex, string? zoneRegex, string? ledRegex, float value, int frameCount, float transitionSpeed)
    {
      Color baseColor = ParseHex(ColorHex);
      float intensity = Math.Clamp(value / 100f, 0.0f, 1.0f);
      Color targetColor = new(
                (byte)(baseColor.R * intensity),
                (byte)(baseColor.G * intensity),
                (byte)(baseColor.B * intensity)
            );

      Color[] colors = client.GetControllerData(deviceIndex).Colors;
      // The call takes exactly its 5 parameters
      ApplyToTargetLeds(device, zoneRegex, ledRegex, colors, targetColor, transitionSpeed);
      client.UpdateLeds(deviceIndex, colors);
    }

    private static Color ParseHex(string hex)
    {
      hex = hex.Replace("#", "");
      if (hex.Length != 6) return new Color(255, 255, 255);
      return new Color(
          Convert.ToByte(hex.Substring(0, 2), 16),
          Convert.ToByte(hex.Substring(2, 2), 16),
          Convert.ToByte(hex.Substring(4, 2), 16)
      );
    }
  }
}