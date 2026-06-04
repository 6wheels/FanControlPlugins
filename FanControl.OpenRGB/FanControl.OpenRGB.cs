using FanControl.OpenRGB.Rules;
using FanControl.Plugins;
using OpenRGB.NET;

namespace FanControl.OpenRGB
{
  public class OpenRgbPlugin : IPlugin
  {
    public string Name => "OpenRGB";

    private OpenRgbClient _client = null!;
    private System.Timers.Timer _renderLoop = null!;
    private bool _isConnected = false;
    private int _frameCount = 0;

    // Les capteurs virtuels qui apparaîtront dans FanControl
    private RgbVirtualControl _ctrlLiquidTemp = null!;
    private RgbVirtualControl _ctrlCpuTemp = null!;
    private RgbVirtualControl _ctrlGpuTemp = null!;
    private RgbVirtualControl _ctrlCaseTemp = null!;

    // Animation de démarrage
    private IRgbRule _bootAnimation = null!;

    private static string GetLockFilePath() => Path.Combine(Path.GetTempPath(), "fancontrol_rgb.lock");

    // Structure pour lier une règle à un capteur
    private class RuleBinding
    {
      public IRgbRule Rule { get; set; } = null!;
      public RgbVirtualControl Control { get; set; } = null!;
    }
    private List<RuleBinding> _bindings = null!;

    public void Initialize()
    {
      try
      {
        _client = new OpenRgbClient(name: "OpenRGB");
        _client.Connect();
        _isConnected = true;

        // Initialisation de l'animation de boot (Cible le clavier)
        _bootAnimation = new BootExplosionRule(".*Alloy.*");

        _renderLoop = new System.Timers.Timer(33); // 30 FPS
        _renderLoop.Elapsed += RenderLoop_Tick;
        _renderLoop.Start();
      }
      catch { _isConnected = false; }
    }

    public void Load(IPluginSensorsContainer container)
    {
      // 1. Déclaration des capteurs virtuels dans l'UI de FanControl
      _ctrlLiquidTemp = new RgbVirtualControl("RGB_LIQUID", "RGB Liquid Temp");
      _ctrlCpuTemp = new RgbVirtualControl("RGB_CPU", "RGB CPU Temp");
      _ctrlGpuTemp = new RgbVirtualControl("RGB_GPU", "RGB GPU Temp");
      _ctrlCaseTemp = new RgbVirtualControl("RGB_CASE", "RGB Case Temp");

      container.ControlSensors.Add(_ctrlLiquidTemp);
      container.ControlSensors.Add(_ctrlCpuTemp);
      container.ControlSensors.Add(_ctrlGpuTemp);
      container.ControlSensors.Add(_ctrlCaseTemp);

      // 2. Mapping des touches pour le mode jeu (Exemple d'indices à vérifier avec ton Scanner)
      var gameKeys = new Dictionary<int, Color>
            {
                { 30, new Color(255, 255, 0) }, // W (Index à ajuster)
                { 44, new Color(255, 255, 0) }, // A
                { 45, new Color(255, 255, 0) }, // S
                { 46, new Color(255, 255, 0) }, // D
                { 58, new Color(0, 255, 0) },   // LShift
                { 72, new Color(0, 255, 0) },   // LCtrl
                { 75, new Color(0, 255, 0) }    // Space
            };

      // Index des touches F1 à F12 (A vérifier avec le menu 1 de ton script console !)
      int[] keysF1_F4 = [8, 9, 10, 11];
      int[] keysF5_F8 = [12, 13, 14, 15];
      int[] keysF9_F12 = [16, 17, 18, 19];

      // 3. Déclaration des liaisons (L'ORDRE EST CRUCIAL POUR LE CLAVIER !)
      _bindings =
            [
                // Kraken X63 : Zone 1 (Ring) sur Temp Liquide
                new() { Control = _ctrlLiquidTemp, Rule = new ZoneThermalGradientRule(".*Kraken.*", "Ring", new Color(0, 50, 255), new Color(255, 0, 0)) },
                
                // Kraken X63 : Zone 0 (Logo) sur Temp CPU
                new() { Control = _ctrlCpuTemp, Rule = new ZoneThermalGradientRule(".*Kraken.*", "Logo", new Color(255, 255, 0), new Color(255, 0, 255)) },

                // Boitier Smart Device : Respiration sur Temp Boitier
                new() { Control = _ctrlCaseTemp, Rule = new BreathingThermalRule(".*Smart Device.*", new Color(0, 50, 255), new Color(255, 0, 0)) },

                // CLAVIER (Couches superposées de bas en haut)
                // Couche 1 : Mode Jeu activé si GPU > 50% (ex: 50°C sur ta courbe)
                new() { Control = _ctrlGpuTemp, Rule = new GameModeRule(".*Alloy.*", 50f, gameKeys) },
                
                // Couche 2 : Les jauges qui s'impriment par-dessus le fond
                new() { Control = _ctrlLiquidTemp, Rule = new LedGaugeRule(".*Alloy.*", keysF1_F4) },
                new() { Control = _ctrlCpuTemp,    Rule = new LedGaugeRule(".*Alloy.*", keysF5_F8) },
                new() { Control = _ctrlGpuTemp,    Rule = new LedGaugeRule(".*Alloy.*", keysF9_F12) }
            ];
    }

    private void RenderLoop_Tick(object? sender, System.Timers.ElapsedEventArgs e)
    {
      if (!_isConnected) return;

      // Gestion du Lock File (Pour tes tests)
      if (File.Exists(GetLockFilePath())) return;

      var devices = _client.GetAllControllerData();

      // Séquence de démarrage prioritaire
      if (!_bootAnimation.IsFinished)
      {
        // On simule une valeur à 100 pour la balle
        _bootAnimation.Apply(_client, devices, 100f, _frameCount);
      }
      else
      {
        // Comportement normal hiérarchique
        foreach (var binding in _bindings)
        {
          // Chaque règle reçoit la valeur de son propre capteur FanControl
          binding.Rule.Apply(_client, devices, binding.Control.Value ?? 0f, _frameCount);
        }
      }

      _frameCount++;
    }

    public void Close()
    {
      _renderLoop?.Stop();
      _client?.Dispose();
    }
  }
}