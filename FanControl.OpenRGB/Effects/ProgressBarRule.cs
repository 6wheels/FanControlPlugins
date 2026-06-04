using FanControl.OpenRGB.Rules;
using OpenRGB.NET;

namespace FanControl.OpenRGB.Effects
{
  // EFFECT 4: Jauge de remplissage linéaire (Barre de progression)
  public class ProgressBarRule(string deviceRegex, Color filledColor, Color emptyColor) : BaseRgbRule(deviceRegex)
  {
    private readonly Color _filledColor = filledColor;
    private readonly Color _emptyColor = emptyColor;

    protected override void ProcessEffect(OpenRgbClient client, Device device, int deviceIndex, float value, int frameCount)
    {
      // On s'assure que la valeur est entre 0 et 100
      float ratio = Math.Clamp(value / 100f, 0.0f, 1.0f);

      // On calcule combien de LEDs doivent être allumées
      int ledsToLight = (int)(device.Leds.Length * ratio);

      Color[] colors = new Color[device.Leds.Length];

      // On remplit la jauge
      for (int i = 0; i < colors.Length; i++)
      {
        colors[i] = (i < ledsToLight) ? _filledColor : _emptyColor;
      }

      client.UpdateLeds(deviceIndex, colors);
    }
  }
}