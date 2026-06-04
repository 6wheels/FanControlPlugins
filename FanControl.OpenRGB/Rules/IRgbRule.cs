using System.Text.RegularExpressions;
using FanControl.OpenRGB.Effects;
using OpenRGB.NET;

namespace FanControl.OpenRGB.Rules
{
  public interface IRgbRule
  {
    // On retourne l'état de la règle (utile pour l'animation de boot)
    bool IsFinished { get; }
    void Apply(OpenRgbClient client, Device[] devices, float currentValue, int frameCount);
  }

  public abstract class BaseRgbRule : IRgbRule
  {
    protected string DeviceRegex { get; }
    protected string? ZoneRegex { get; } // Optionnel : pour cibler une zone précise
    public bool IsFinished { get; protected set; } = false;

    protected BaseRgbRule(string deviceRegex, string? zoneRegex = null)
    {
      DeviceRegex = deviceRegex;
      ZoneRegex = zoneRegex;
    }

    public void Apply(OpenRgbClient client, Device[] devices, float currentValue, int frameCount)
    {
      if (IsFinished) return;

      for (int i = 0; i < devices.Length; i++)
      {
        var device = devices[i];
        if (Regex.IsMatch(device.Name ?? "", DeviceRegex))
        {
          ProcessEffect(client, device, i, currentValue, frameCount);
        }
      }
    }

    protected abstract void ProcessEffect(OpenRgbClient client, Device device, int deviceIndex, float value, int frameCount);
  }

  // --- REGLE 1 : L'animation de boot (Modifiée pour s'arrêter) ---
  public class BootExplosionRule(string deviceRegex) : BouncingExplosionRule(deviceRegex)
  {
    // On override juste la fin du fade pour tuer l'animation
    protected override void OnCycleComplete()
    {
      IsFinished = true; // Stoppe l'exécution aux prochaines frames
    }
  }

  // --- REGLE 2 : Dégradé ciblé par Zone (Kraken) ---
  public class ZoneThermalGradientRule(string deviceRegex, string zoneRegex, Color colorMin, Color colorMax) : BaseRgbRule(deviceRegex, zoneRegex)
  {
    private Color _colorMin = colorMin;
    private Color _colorMax = colorMax;

    protected override void ProcessEffect(OpenRgbClient client, Device device, int deviceIndex, float value, int frameCount)
    {
      float ratio = Math.Clamp(value / 100f, 0.0f, 1.0f);
      byte r = (byte)(_colorMin.R + (_colorMax.R - _colorMin.R) * ratio);
      byte g = (byte)(_colorMin.G + (_colorMax.G - _colorMin.G) * ratio);
      byte b = (byte)(_colorMin.B + (_colorMax.B - _colorMin.B) * ratio);
      Color targetColor = new(r, g, b);

      Color[] colors = client.GetControllerData(deviceIndex).Colors; // Récupère l'état actuel des couleurs !

      int ledOffset = 0;
      foreach (var zone in device.Zones)
      {
        if (Regex.IsMatch(zone.Name, ZoneRegex!))
        {
          for (int l = 0; l < zone.LedCount; l++) colors[ledOffset + l] = targetColor;
        }
        ledOffset += (int)zone.LedCount;
      }
      client.UpdateLeds(deviceIndex, colors);
    }
  }

  // --- REGLE 3 : Respiration Thermale (Smart Device / Boitier) ---
  public class BreathingThermalRule : BaseRgbRule
  {
    private Color _colorMin;
    private Color _colorMax;

    public BreathingThermalRule(string deviceRegex, Color min, Color max) : base(deviceRegex)
    {
      _colorMin = min; _colorMax = max;
    }

    protected override void ProcessEffect(OpenRgbClient client, Device device, int deviceIndex, float value, int frameCount)
    {
      float ratio = Math.Clamp(value / 100f, 0.0f, 1.0f);
      byte r = (byte)(_colorMin.R + (_colorMax.R - _colorMin.R) * ratio);
      byte b = (byte)(_colorMin.B + (_colorMax.B - _colorMin.B) * ratio);

      // Respiration lente indépendante de la charge
      float pulse = (float)(Math.Sin(frameCount * 0.03) + 1.0) / 2.0f;

      Color targetColor = new((byte)(r * pulse), 0, (byte)(b * pulse));
      client.UpdateLeds(deviceIndex, Enumerable.Repeat(targetColor, device.Leds.Length).ToArray());
    }
  }

  // --- REGLE 4 : Le "Game Mode" (Clavier Fond + WASD) ---
  public class GameModeRule(string deviceRegex, float triggerThreshold, Dictionary<int, Color> specificKeys) : BaseRgbRule(deviceRegex)
  {
    private float _threshold = triggerThreshold;
    private Dictionary<int, Color> _specificKeys = specificKeys;

    protected override void ProcessEffect(OpenRgbClient client, Device device, int deviceIndex, float value, int frameCount)
    {
      Color[] colors = client.GetControllerData(deviceIndex).Colors; // Conserve l'état précédent

      if (value >= _threshold)
      {
        // 1. Fond rouge partout
        for (int i = 0; i < colors.Length; i++) colors[i] = new Color(100, 0, 0);

        // 2. Touches spécifiques par dessus
        foreach (var kvp in _specificKeys)
        {
          if (kvp.Key < colors.Length) colors[kvp.Key] = kvp.Value;
        }
      }
      else
      {
        // Mode Bureau : tout éteint (ou une autre couleur de ton choix)
        for (int i = 0; i < colors.Length; i++) colors[i] = new Color(0, 0, 0);
      }
      client.UpdateLeds(deviceIndex, colors);
    }
  }

  // --- REGLE 5 : Les Jauges Thermales (F1-F4, etc.) ---
  public class LedGaugeRule(string deviceRegex, int[] ledIndices) : BaseRgbRule(deviceRegex)
  {
    private int[] _ledIndices = ledIndices;

    protected override void ProcessEffect(OpenRgbClient client, Device device, int deviceIndex, float value, int frameCount)
    {
      Color[] colors = client.GetControllerData(deviceIndex).Colors; // Ne touche pas au reste du clavier !

      float ratio = Math.Clamp(value / 100f, 0.0f, 1.0f);
      int ledsToLight = (int)Math.Round(_ledIndices.Length * ratio);

      // Calcul de la couleur de la jauge (Vert -> Jaune -> Rouge)
      Color gaugeColor;
      if (ratio < 0.5f) gaugeColor = new Color((byte)(255 * (ratio * 2)), 255, 0); // Vert vers Jaune
      else gaugeColor = new Color(255, (byte)(255 * (1f - (ratio - 0.5f) * 2)), 0); // Jaune vers Rouge

      for (int i = 0; i < _ledIndices.Length; i++)
      {
        int ledIdx = _ledIndices[i];
        if (ledIdx < colors.Length)
        {
          colors[ledIdx] = (i < ledsToLight) ? gaugeColor : new Color(0, 0, 0);
        }
      }
      client.UpdateLeds(deviceIndex, colors);
    }
  }
}