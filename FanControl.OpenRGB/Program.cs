using System.Globalization;
using System.Reflection;
using FanControl.OpenRGB.Effects;
using OpenRGB.NET;

namespace FanControl.OpenRGB
{
  class Program
  {
    // Chemin du fichier de verrouillage partagé avec le plugin
    private static string LockFilePath => Path.Combine(Path.GetTempPath(), "fancontrol_rgb.lock");

    static void Main(string[] args)
    {
      Console.OutputEncoding = System.Text.Encoding.UTF8;
      Console.WriteLine("===========================================");
      Console.WriteLine("⚙️  OPENRGB PLUGIN - DEV TOOLKIT");
      Console.WriteLine("===========================================\n");

      // 1. Création du fichier Lock pour dire au plugin FanControl de se mettre en pause
      try { File.Create(LockFilePath).Dispose(); } catch { }
      Console.WriteLine("🔒 Lock file créé. FanControl est en pause.");

      OpenRgbClient? client = null;

      try
      {
        string ip = "127.0.0.1";
        int port = 6742; // Port par défaut du Daemon OpenRGB
        bool isConnected = false;

        // --- BOUCLE DE CONNEXION ---
        while (!isConnected)
        {
          try
          {
            Console.WriteLine($"\nTentative de connexion à {ip}:{port}...");
            client = new OpenRgbClient(name: "Console Toolkit", ip: ip, port: port);
            client.Connect();
            isConnected = true;
            Console.WriteLine("✅ Connecté au serveur OpenRGB.");

            // FEEDBACK VISUEL : Tout le setup passe en Bleu Nuit (R:0, G:50, B:150)
            SetAllHardwareColor(client, new Color(0, 50, 150));
            Console.WriteLine("💡 Contrôle matériel confirmé (Setup éclairé en Bleu).");

            Thread.Sleep(1000); // Petite pause pour la lisibilité avant d'afficher le menu
          }
          catch (Exception ex)
          {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"❌ Échec de la connexion : {ex.Message}");
            Console.ResetColor();

            Console.WriteLine("\nVoulez-vous essayer un autre port ? (O/N) - [ECHAP] pour quitter.");
            var key = Console.ReadKey(true);

            if (key.Key == ConsoleKey.Escape || char.ToUpper(key.KeyChar) == 'N')
            {
              return; // Quitte le Main() et passe directement dans le bloc finally
            }
            else if (char.ToUpper(key.KeyChar) == 'O')
            {
              Console.WriteLine("\n");
              Console.Write("Entrez le port (ex: 6742, 6789) : ");
              string? inputPort = Console.ReadLine();
              if (int.TryParse(inputPort, out int newPort))
              {
                port = newPort;
              }
              else
              {
                Console.WriteLine("Port invalide, nouvel essai avec le port " + port);
              }
            }
          }
        }

        // --- MENU PRINCIPAL ---
        bool isRunning = true;
        while (isRunning)
        {
          Console.WriteLine("\n");
          Console.WriteLine("Choose an option:");
          Console.WriteLine("-------------------------------------------");
          Console.WriteLine("[1] Scan Hardware (Detect Devices, Zones & LEDs)");
          Console.WriteLine("[2] Test Effects (Static, Aurora, Color Wipe, Breathing, etc.)");
          Console.WriteLine("[ESC] Exit");
          Console.WriteLine("--------------------------------------------");
          Console.Write("Select an option (1, or ESC): \n");

          var input = Console.ReadKey(true);

          if (input.Key == ConsoleKey.Escape) break;

          switch (input.KeyChar)
          {
            case '1': RunHardwareScanner(client!); break;
            case '2': TestEffectsMenu(client!); break;
          }
        }
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
        // On s'assure de couper le bleu de debug (ou de tout éteindre) avant de rendre la main
        if (client != null && client.Connected)
        {
          SetAllHardwareColor(client, new Color(0, 0, 0)); // Noir (Éteint)
          client.Dispose();
        }

        // Suppression garantie du fichier Lock à la sortie
        if (File.Exists(LockFilePath)) File.Delete(LockFilePath);
        Console.WriteLine("\n🔓 Lock file supprimé. FanControl reprend la main.");
      }
    }

    private static void RunHardwareScanner(OpenRgbClient client)
    {
      var devices = client.GetAllControllerData();
      Console.WriteLine("\n");
      Console.WriteLine($"🔍 HARDWARE SCANNER - {devices.Length} DEVICE(S) FOUND");
      Console.WriteLine("--------------------------------------------\n");

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

      Console.WriteLine("\nPress [ESCAPE] to return to Main Menu...");
      while (Console.ReadKey(true).Key != ConsoleKey.Escape) { }
    }

    private static void TestEffectsMenu(OpenRgbClient client)
    {
      Console.Clear();

      List<Type> effectTypes = [];

      try
      {
        // Tente de charger tous les types
        effectTypes = Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => t.IsSubclassOf(typeof(BaseRgbEffect)) && !t.IsAbstract)
            .ToList();
      }
      catch (ReflectionTypeLoadException ex)
      {
        // Si la DLL FanControl manque (ce qui est normal en mode Console), 
        // on récupère uniquement les classes qui ont réussi à se charger (nos Effets).
        effectTypes = ex.Types
            .Where(t => t != null && t.IsSubclassOf(typeof(BaseRgbEffect)) && !t.IsAbstract)
            .Cast<Type>()
            .ToList();
      }

      Console.WriteLine("===========================================");
      Console.WriteLine("🧪 TESTEUR D'EFFETS (INTROSPECTION)");
      Console.WriteLine("===========================================\n");

      // ... (Le reste de ta méthode reste exactement identique)
      for (int i = 0; i < effectTypes.Count; i++)
      {
        Console.WriteLine($"[{i}] {effectTypes[i].Name}");
      }
      Console.WriteLine($"\n[ESC] Retour au menu principal");
      Console.WriteLine("===========================================");

      var input = Console.ReadKey(true);
      if (input.Key == ConsoleKey.Escape) return;

      // Si l'utilisateur tape un chiffre correspondant à un effet
      if (int.TryParse(input.KeyChar.ToString(), out int index) && index >= 0 && index < effectTypes.Count)
      {
        RunEffectTest(client, effectTypes[index]);
      }
    }

    private static void RunEffectTest(OpenRgbClient client, Type effectType)
    {
      Console.Clear();
      Console.WriteLine($"=== CONFIGURATION DE : {effectType.Name} ===\n");

      // On instancie la classe dynamiquement
      var effect = (BaseRgbEffect)Activator.CreateInstance(effectType)!;

      // INTROSPECTION : On récupère uniquement les propriétés spécifiques à cet effet (DeclaredOnly)
      var properties = effectType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

      foreach (var prop in properties)
      {
        object? defaultValue = prop.GetValue(effect);
        Console.Write($"- {prop.Name} ({prop.PropertyType.Name}) [Défaut: {defaultValue}] : ");

        string? userInput = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(userInput))
        {
          try
          {
            object convertedValue;
            if (prop.PropertyType.IsEnum)
            {
              // Gère les enums comme AuroraDirection
              convertedValue = Enum.Parse(prop.PropertyType, userInput, true);
            }
            else
            {
              // Gère les floats de manière robuste (accepte point ou virgule)
              string safeInput = userInput.Replace(",", ".");
              convertedValue = Convert.ChangeType(safeInput, prop.PropertyType, CultureInfo.InvariantCulture);
            }
            prop.SetValue(effect, convertedValue);
          }
          catch
          {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  -> Saisie invalide. Conservation de la valeur par défaut : {defaultValue}");
            Console.ResetColor();
          }
        }
      }

      Console.WriteLine("\n▶ Lancement de la boucle de rendu... Appuyez sur une touche pour arrêter.");

      var devices = client.GetAllControllerData();
      int frameCount = 0;

      // Boucle non-bloquante pour jouer l'animation à ~30 FPS
      while (!Console.KeyAvailable)
      {
        effect.Apply(client, devices, ".*", null, 100f, frameCount);
        frameCount++;
        Thread.Sleep(33);
      }

      Console.ReadKey(true); // Consomme la touche pressée pour nettoyer le buffer
    }

    private static void SetAllHardwareColor(OpenRgbClient client, global::OpenRGB.NET.Color color)
    {
      try
      {
        var devices = client.GetAllControllerData();
        for (int i = 0; i < devices.Length; i++)
        {
          // On remplit un tableau avec la couleur demandée pour chaque LED du périphérique
          var colors = Enumerable.Repeat(color, devices[i].Leds.Length).ToArray();
          client.UpdateLeds(i, colors);
        }
      }
      catch { }
    }

  }
}