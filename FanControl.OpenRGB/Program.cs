using OpenRGB.NET;

namespace FanControl.OpenRGB
{
  class Program
  {
    // Chemin du fichier de verrouillage partagé avec le plugin
    private static string LockFilePath => Path.Combine(Path.GetTempPath(), "fancontrol_rgb.lock");

    static void Main(string[] args)
    {
      Console.WriteLine("===========================================");
      Console.WriteLine("⚙️ OPENRGB PLUGIN - DEV TOOLKIT");
      Console.WriteLine("===========================================\n");

      // 1. Création du fichier Lock pour dire au plugin FanControl de se mettre en pause
      try { File.Create(LockFilePath).Dispose(); } catch { }
      Console.WriteLine("🔒 Lock file créé. FanControl est en pause.");

      try
      {
        // Sans port précisé, OpenRGB.NET utilise le 6742 par défaut (le bon port du Daemon !)
        using var client = new OpenRgbClient(name: "Console Toolkit");
        client.Connect();
        Console.WriteLine("✅ Connecté au serveur OpenRGB.");

        RunHardwareScanner(client);
      }
      catch (Exception ex)
      {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"\n❌ FATAL ERROR: {ex.Message}");
        Console.ResetColor();
        Console.ReadKey();
      }
      finally
      {
        // 2. Suppression garantie du fichier Lock à la sortie
        if (File.Exists(LockFilePath)) File.Delete(LockFilePath);
        Console.WriteLine("🔓 Lock file supprimé. FanControl reprend la main.");
      }
    }

    private static void RunHardwareScanner(OpenRgbClient client)
    {
      Console.Clear();
      var devices = client.GetAllControllerData();
      Console.WriteLine("===========================================");
      Console.WriteLine($"🔍 HARDWARE SCANNER - {devices.Length} DEVICE(S) FOUND");
      Console.WriteLine("===========================================\n");

      for (int i = 0; i < devices.Length; i++)
      {
        var device = devices[i];

        // --- 1. INFOS DU PÉRIPHÉRIQUE ---
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"[{i}] DEVICE: {device.Name}");
        Console.ResetColor();

        Console.WriteLine($"    Type        : {device.Type}");
        Console.WriteLine($"    Description : {device.Description}");
        Console.WriteLine($"    Version     : {device.Version}");
        Console.WriteLine($"    Serial      : {device.Serial}");
        Console.WriteLine($"    Location    : {device.Location}");
        Console.WriteLine($"    Total LEDs  : {device.Leds.Length}");

        // --- 2. MODES DISPONIBLES ---
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine($"\n    ▶ MODES ({device.Modes.Length}):");
        Console.ResetColor();

        for (int m = 0; m < device.Modes.Length; m++)
        {
          var mode = device.Modes[m];
          string activeTag = (m == device.ActiveModeIndex) ? "[ACTIF]" : "       ";
          Console.WriteLine($"      {activeTag} [{m}] {mode.Name,-20} (Speed: {mode.SpeedMin}-{mode.SpeedMax}, Flags: {mode.Flags})");
        }

        // --- 3. ZONES ET LEDS ---
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"\n    ▶ ZONES ({device.Zones.Length}):");
        Console.ResetColor();

        int ledGlobalOffset = 0; // Sert à faire la correspondance entre les LEDs de la zone et le tableau global

        for (int z = 0; z < device.Zones.Length; z++)
        {
          var zone = device.Zones[z];
          Console.ForegroundColor = ConsoleColor.Yellow;
          Console.WriteLine($"      - [Zone {z}] {zone.Name}");
          Console.ResetColor();

          Console.WriteLine($"        Type: {zone.Type} | Nb LEDs: {zone.LedCount}");

          // Si la zone est une matrice (ex: Clavier), on affiche ses dimensions
          if (zone.MatrixMap != null)
          {
            Console.WriteLine($"        Matrix Map: {zone.MatrixMap.Width}x{zone.MatrixMap.Height}");
          }

          // Détail des LEDs de cette zone
          Console.ForegroundColor = ConsoleColor.DarkGray;
          Console.WriteLine("        Détail des LEDs :");
          for (int l = 0; l < zone.LedCount; l++)
          {
            if (ledGlobalOffset < device.Leds.Length)
            {
              var led = device.Leds[ledGlobalOffset];
              var color = device.Colors[ledGlobalOffset];

              // On affiche l'index global, le nom de la LED, et sa couleur actuelle
              Console.WriteLine($"          [{ledGlobalOffset,3}] {led.Name,-25} -> RGB({color.R,3}, {color.G,3}, {color.B,3})");

              ledGlobalOffset++;
            }
          }
          Console.ResetColor();
          Console.WriteLine();
        }

        Console.WriteLine(new string('-', 60));
        Console.WriteLine();
      }
    }
  }
}