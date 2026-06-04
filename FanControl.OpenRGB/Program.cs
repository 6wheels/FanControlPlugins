using FanControl.OpenRGB.Rules;
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
        using var client = new OpenRgbClient(name: "Console Toolkit");
        client.Connect();
        Console.WriteLine("✅ Connecté au serveur OpenRGB.");

        bool isRunning = true;
        while (isRunning)
        {
          Console.Clear();
          Console.WriteLine("===========================================");
          Console.WriteLine(" MAIN MENU");
          Console.WriteLine("===========================================");
          Console.WriteLine("[1] Scan Hardware (Detect Devices & Zones)");
          Console.WriteLine("[2] Test Animations & Rules");
          Console.WriteLine("[ESC] Exit");
          Console.WriteLine("===========================================");
          Console.Write("Select an option (1, 2, or ESC): ");

          var input = Console.ReadKey(true);

          if (input.Key == ConsoleKey.Escape) break;

          switch (input.KeyChar)
          {
            case '1': RunHardwareScanner(client); break;
            case '2': RunAnimationTestBench(client); break;
          }
        }

        SetAllToDarkGreen(client);
        Console.WriteLine("\n🛑 Hardware set to Dark Green.");
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
      Console.WriteLine($"Number of detected devices: {devices.Length}\n");

      for (int i = 0; i < devices.Length; i++)
      {
        var device = devices[i];
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"[{i}] DEVICE: {device.Name}");
        Console.ResetColor();

        for (int z = 0; z < device.Zones.Length; z++)
        {
          var zone = device.Zones[z];
          Console.ForegroundColor = ConsoleColor.Yellow;
          Console.WriteLine($"      -> [Zone {z}] {zone.Name} ({zone.LedCount} LEDs)");
          Console.ResetColor();
        }
        Console.WriteLine("-------------------------------------------");
      }
      Console.WriteLine("\nPress [ESCAPE] to return...");
      while (Console.ReadKey(true).Key != ConsoleKey.Escape) { }
    }

    private static void RunAnimationTestBench(OpenRgbClient client)
    {
      Console.Clear();
      var devices = client.GetAllControllerData();

      Console.WriteLine("Available Animations:");
      Console.WriteLine("  [1] Boot Explosion (One cycle then stop)");
      Console.WriteLine("  [2] Zone Thermal Gradient (Kraken Ring Demo)");
      Console.WriteLine("  [3] Breathing Thermal (Case Demo)");
      Console.WriteLine("  [4] LED Gauges (F1-F4 Demo)");
      Console.WriteLine("  [5] Game Mode (WASD Demo)");
      Console.Write("\nSelect an animation (1-5) or [ESC] to cancel: ");

      var animInput = Console.ReadKey(true);
      if (animInput.Key == ConsoleKey.Escape) return;

      Console.WriteLine("\n\nAvailable Target Devices:");
      Console.WriteLine("  [A] All Devices");
      for (int i = 0; i < devices.Length; i++)
      {
        if (i <= 9) Console.WriteLine($"  [{i}] {devices[i].Name}");
      }
      Console.Write("\nSelect a target (A for All, 0-9): ");

      var targetInput = Console.ReadKey(true);
      if (targetInput.Key == ConsoleKey.Escape) return;
      string targetStr = targetInput.KeyChar.ToString().ToUpper();

      string targetRegex = targetStr == "A" ? ".*" :
          (int.TryParse(targetStr, out int idx) && idx < devices.Length) ?
          System.Text.RegularExpressions.Regex.Escape(devices[idx].Name ?? "") : "";

      if (string.IsNullOrEmpty(targetRegex)) return;

      IRgbRule selectedRule;
      switch (animInput.KeyChar)
      {
        case '1':
          selectedRule = new BootExplosionRule(targetRegex);
          break;
        case '2': // Démo sur le nom de zone "Ring" par défaut
          selectedRule = new ZoneThermalGradientRule(targetRegex, "Ring", new Color(0, 50, 255), new Color(255, 0, 0));
          break;
        case '3':
          selectedRule = new BreathingThermalRule(targetRegex, new Color(0, 50, 255), new Color(255, 0, 0));
          break;
        case '4': // Démo sur les 4 premières touches du clavier
          selectedRule = new LedGaugeRule(targetRegex, new int[] { 8, 9, 10, 11 });
          break;
        case '5': // Démo Game Mode
          var gameKeys = new Dictionary<int, Color> { { 30, new Color(255, 255, 0) }, { 44, new Color(255, 255, 0) } };
          selectedRule = new GameModeRule(targetRegex, 50f, gameKeys);
          break;
        default:
          return;
      }

      Console.Clear();
      Console.WriteLine("🔄 Running animation...");
      Console.WriteLine("Press [ESCAPE] to stop and return to menu.\n");

      int frameCount = 0;
      while (Console.KeyAvailable) Console.ReadKey(true);

      while (!(Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.Escape))
      {
        // Si la règle a un statut IsFinished (ex: BootExplosion), on arrête la boucle
        if (selectedRule.IsFinished)
        {
          Console.WriteLine("\n✅ Sequence finished automatically.");
          break;
        }

        float mockValue = (float)(Math.Sin(frameCount * 0.05) + 1.0) / 2.0f * 100f;
        Console.Write($"\rFrame: {frameCount:D4} | Simulated Input (0-100%): {mockValue:F1}%   ");

        selectedRule.Apply(client, devices, mockValue, frameCount);

        Thread.Sleep(33);
        frameCount++;
      }

      Console.WriteLine("\n\n🛑 Resetting to Dark Green...");
      SetAllToDarkGreen(client);
      Console.WriteLine("Press [ESCAPE] to return to the main menu...");
      while (Console.KeyAvailable) Console.ReadKey(true);
      while (Console.ReadKey(true).Key != ConsoleKey.Escape) { }
    }

    private static void SetAllToDarkGreen(OpenRgbClient client)
    {
      try
      {
        var devices = client.GetAllControllerData();
        for (int i = 0; i < devices.Length; i++)
        {
          var colors = Enumerable.Repeat(new Color(0, 50, 0), devices[i].Leds.Length).ToArray();
          client.UpdateLeds(i, colors);
        }
      }
      catch { }
    }
  }
}