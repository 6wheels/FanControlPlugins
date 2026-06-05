using System.Text.Json;
using System.Text.RegularExpressions;
using FanControl.Plugins;
using OpenRGB.NET;
using FanControl.OpenRGB.Rules;
using FanControl.OpenRGB.Effects;

namespace FanControl.OpenRGB
{
  public class OpenRgbPlugin(IPluginDialog dialog, IPluginLogger logger) : IPlugin
  {
    public string Name => "OpenRGB";

    private OpenRgbClient? _client;
    private Timer? _renderTimer;
    private Device[] _devices = [];

    private OpenRgbConfig _config = new();
    private readonly string _configPath = "OpenRGBConfig.json";
    private readonly string _lockFilePath = Path.Combine(Path.GetTempPath(), "fancontrol_rgb.lock");
    private bool _isLocked = false;
    private readonly List<RuleBinding> _bindings = [];
    private int _frameCount = 0;

    public void Initialize()
    {
      if (File.Exists(_configPath))
      {
        try
        {
          var options = new JsonSerializerOptions { ReadCommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true };
          string json = File.ReadAllText(_configPath);
          _config = JsonSerializer.Deserialize<OpenRgbConfig>(json, options) ?? new OpenRgbConfig();

          Log($"Configuration loaded. {_config.Rules.Count} rule(s) found.", LogLevel.Info);

          string parsedConfigJson = JsonSerializer.Serialize(_config, new JsonSerializerOptions { WriteIndented = true });
          Log($"Parsed configuration dump:\n{parsedConfigJson}", LogLevel.Debug);
          // -------------------------------------------------
          Log($"Server IP: {_config.ServerIp}, Port: {_config.ServerPort}, Framerate: {_config.Framerate} FPS, LogLevel: {_config.LogLevel}", LogLevel.Debug);
        }
        catch (Exception ex)
        {
          Log($"Configuration failed: {ex.Message}", LogLevel.Error);
        }
      }
      else
      {
        File.WriteAllText(_configPath, JsonSerializer.Serialize(new OpenRgbConfig(), new JsonSerializerOptions { WriteIndented = true }));
        Log("Template configuration file generated. Please configure it.", LogLevel.Info);
        dialog.ShowMessageDialog("OpenRGB: Template configuration file generated. Please configure it.");
        return;
      }

      try
      {
        _client = new OpenRgbClient(name: "FanControl", ip: _config.ServerIp, port: _config.ServerPort);
        _client.Connect();
        _devices = _client.GetAllControllerData();

        // LOG 1 : INVENTAIRE DES PÉRIPHÉRIQUES
        Log($"Connected. {_devices.Length} detected devices:");
        for (int i = 0; i < _devices.Length; i++)
        {
          Log($"Device [{i}] : '{_devices[i].Name}', Available Zones: {_devices[i].Zones})");
        }

        int interval = 1000 / _config.Framerate;
        _renderTimer = new Timer(RenderLoop_Tick, null, 0, interval);
      }
      catch (Exception ex)
      {
        Log($"Connexion failed: {ex.Message}", LogLevel.Error);
      }
    }

    public void Load(IPluginSensorsContainer container)
    {
      _bindings.Clear();

      foreach (var ruleConf in _config.Rules)
      {
        string safeId = string.IsNullOrWhiteSpace(ruleConf.Id)
            ? $"OPENRGB_{ruleConf.Name.Replace(" ", "_").ToUpper()}"
            : ruleConf.Id;

        var controlSensor = new OpenRgbControlSensor(safeId, ruleConf.Name);

        // Beaucoup plus propre : on passe juste l'objet config et le capteur
        var binding = new RuleBinding(ruleConf, controlSensor);

        _bindings.Add(binding);
        container.ControlSensors.Add(controlSensor);
        int matchCount = _devices.Count(d => Regex.IsMatch(d.Name ?? "", ruleConf.DeviceRegex));
        Log($"Card '{ruleConf.Name}' created. Regex '{ruleConf.DeviceRegex}' matches {matchCount} device(s).", LogLevel.Info);
      }
    }

    private void RenderLoop_Tick(object? state)
    {
      // --- GESTION DU VERROUILLAGE (DEV TOOLKIT) ---
      bool lockExists = File.Exists(_lockFilePath);

      if (lockExists)
      {
        if (!_isLocked)
        {
          _isLocked = true;
          Log("Dev Toolkit détecté (Lock file). Mise en pause du plugin...", LogLevel.Warning);
        }
        return; // On annule totalement l'exécution de cette frame
      }
      else if (_isLocked)
      {
        _isLocked = false;
        Log("Dev Toolkit fermé. Reprise du contrôle RGB par FanControl.", LogLevel.Info);
      }
      // ---------------------------------------------

      // On s'assure qu'on est bien connecté avant de continuer
      if (_client == null || !_client.Connected) return;

      _frameCount++;

      // On logue seulement 1 frame toutes les 2 secondes (si 30 FPS)
      bool shouldLogThisFrame = (_frameCount % (_config.Framerate * 2) == 0);

      foreach (var binding in _bindings)
      {
        float val = binding.Control.Value ?? 0f;

        BaseRgbEffect? effectToApply = null;

        if (val >= binding.Config.ActivationThreshold)
        {
          effectToApply = binding.Config.ActiveEffect;
        }
        else if (binding.Config.IdleEffect != null)
        {
          effectToApply = binding.Config.IdleEffect;
        }
        if (shouldLogThisFrame)
        {
          Log($"Card '{binding.Control.Name}' | Value: {val:F1} | Threshold: {binding.Config.ActivationThreshold} | Active Rule: {binding.Config.Name}", LogLevel.Debug);
        }

        effectToApply?.Apply(
            _client,
            _devices,
            binding.Config.DeviceRegex,
            binding.Config.ZoneRegex,
            val,
            _frameCount
        );
      }
    }

    private void Log(string message, LogLevel level = LogLevel.Info)
    {
      // On n'écrit dans le log de FanControl que si le niveau du message 
      // est supérieur ou égal au niveau demandé dans la config.
      if (level >= _config.LogLevel)
      {
        // On ajoute un préfixe visuel si c'est une erreur ou un debug
        string prefix = $"[{level.ToString().ToUpper()}]";

        // 'logger' vient du constructeur primaire de ta classe
        logger.Log($"[OpenRGB] {prefix} {message}");
      }
    }

    public void Close()
    {
      _renderTimer?.Dispose();
      _client?.Dispose();
    }
  }
}