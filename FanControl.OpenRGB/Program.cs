using System.Globalization;
using System.Reflection;
using FanControl.OpenRGB.Effects;
using OpenRGB.NET;

namespace FanControl.OpenRGB
{
  class Program
  {
    // Path of the lock file shared with the plugin
    private static string LockFilePath => Path.Combine(Path.GetTempPath(), "fancontrol_rgb.lock");

    static void Main(string[] args)
    {
      Console.OutputEncoding = System.Text.Encoding.UTF8;
      Console.WriteLine("===========================================");
      Console.WriteLine("⚙️  OPENRGB PLUGIN - DEV TOOLKIT");
      Console.WriteLine("===========================================\n");

      // 1. Create the Lock file to tell the FanControl plugin to pause
      try { File.Create(LockFilePath).Dispose(); } catch { }
      Console.WriteLine("🔒 Lock file created. FanControl is paused.");

      OpenRgbClient? client = null;

      try
      {
        string ip = "127.0.0.1";
        int port = 6742; // Default port of OpenRGB Daemon
        bool isConnected = false;

        // --- CONNECTION LOOP ---
        while (!isConnected)
        {
          try
          {
            Console.WriteLine($"\nAttempting connection to {ip}:{port}...");
            client = new OpenRgbClient(name: "Console Toolkit", ip: ip, port: port);
            client.Connect();
            isConnected = true;
            Console.WriteLine("✅ Connected to OpenRGB server.");

            // VISUAL FEEDBACK: All setup turns Navy Blue (R:0, G:50, B:150)
            SetAllHardwareColor(client, new Color(0, 50, 150));
            Console.WriteLine("💡 Hardware control confirmed (Setup lit in Blue).");

            Thread.Sleep(1000); // Small pause for readability before displaying the menu
          }
          catch (Exception ex)
          {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"❌ Connection failed: {ex.Message}");
            Console.ResetColor();

            Console.WriteLine("\nDo you want to try another port? (Y/N) - [ESC] to exit.");
            var key = Console.ReadKey(true);

            if (key.Key == ConsoleKey.Escape || char.ToUpper(key.KeyChar) == 'N')
            {
              return; // Exit Main() and go directly to the finally block
            }
            else if (char.ToUpper(key.KeyChar) == 'Y')
            {
              Console.WriteLine("\n");
              Console.Write("Enter the port (ex: 6742, 6789): ");
              string? inputPort = Console.ReadLine();
              if (int.TryParse(inputPort, out int newPort))
              {
                port = newPort;
              }
              else
              {
                Console.WriteLine("Invalid port, retrying with port " + port);
              }
            }
          }
        }

        // --- MAIN MENU ---
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
        // We make sure to turn off the debug blue (or turn everything off) before returning control
        if (client != null && client.Connected)
        {
          SetAllHardwareColor(client, new Color(0, 0, 0)); // Black (Off)
          client.Dispose();
        }

        // Guaranteed deletion of Lock file on exit
        if (File.Exists(LockFilePath)) File.Delete(LockFilePath);
        Console.WriteLine("\n🔓 Lock file deleted. FanControl resumes control.");
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

        // --- 1. DEVICE INFO ---
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine($"[{i}] DEVICE: {device.Name}");
        Console.ResetColor();

        Console.WriteLine($"    Type        : {device.Type}");
        Console.WriteLine($"    Description : {device.Description}");
        Console.WriteLine($"    Version     : {device.Version}");
        Console.WriteLine($"    Serial      : {device.Serial}");
        Console.WriteLine($"    Location    : {device.Location}");
        Console.WriteLine($"    Total LEDs  : {device.Leds.Length}");

        // --- 2. AVAILABLE MODES ---
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine($"\n    ▶ MODES ({device.Modes.Length}):");
        Console.ResetColor();

        for (int m = 0; m < device.Modes.Length; m++)
        {
          var mode = device.Modes[m];
          string activeTag = (m == device.ActiveModeIndex) ? "[ACTIVE]" : "       ";
          Console.WriteLine($"      {activeTag} [{m}] {mode.Name,-20} (Speed: {mode.SpeedMin}-{mode.SpeedMax}, Flags: {mode.Flags})");
        }

        // --- 3. ZONES AND LEDS ---
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"\n    ▶ ZONES ({device.Zones.Length}):");
        Console.ResetColor();

        int ledGlobalOffset = 0; // Used to make the correspondence between zone LEDs and the global array

        for (int z = 0; z < device.Zones.Length; z++)
        {
          var zone = device.Zones[z];
          Console.ForegroundColor = ConsoleColor.Yellow;
          Console.WriteLine($"      - [Zone {z}] {zone.Name}");
          Console.ResetColor();

          Console.WriteLine($"        Type: {zone.Type} | LED Count: {zone.LedCount}");

          // If the zone is a matrix (e.g.: Keyboard), we display its dimensions
          if (zone.MatrixMap != null)
          {
            Console.WriteLine($"        Matrix Map: {zone.MatrixMap.Width}x{zone.MatrixMap.Height}");
          }

          // Details of LEDs in this zone
          Console.ForegroundColor = ConsoleColor.DarkGray;
          Console.WriteLine("        LED Details :");
          for (int l = 0; l < zone.LedCount; l++)
          {
            if (ledGlobalOffset < device.Leds.Length)
            {
              var led = device.Leds[ledGlobalOffset];
              var color = device.Colors[ledGlobalOffset];

              // We display the global index, the LED name, and its current color
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
        // Attempts to load all types
        effectTypes = Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => t.IsSubclassOf(typeof(BaseRgbEffect)) && !t.IsAbstract)
            .ToList();
      }
      catch (ReflectionTypeLoadException ex)
      {
        // If the FanControl DLL is missing (which is normal in Console mode),
        // we retrieve only the classes that managed to load (our Effects).
        effectTypes = ex.Types
            .Where(t => t != null && t.IsSubclassOf(typeof(BaseRgbEffect)) && !t.IsAbstract)
            .Cast<Type>()
            .ToList();
      }

      Console.WriteLine("===========================================");
      Console.WriteLine("🧪 EFFECTS TESTER (INTROSPECTION)");
      Console.WriteLine("===========================================\n");

      // ... (The rest of your method remains exactly the same)
      for (int i = 0; i < effectTypes.Count; i++)
      {
        Console.WriteLine($"[{i + 1}] {effectTypes[i].Name}");
      }
      Console.WriteLine($"\n[ESC] Return to main menu");
      Console.WriteLine("===========================================");

      var input = Console.ReadKey(true);
      if (input.Key == ConsoleKey.Escape) return;

      // If the user types a digit corresponding to an effect
      if (int.TryParse(input.KeyChar.ToString(), out int selection) && selection >= 1 && selection <= effectTypes.Count)
      {
        RunEffectTest(client, effectTypes[selection - 1]);
      }
    }

    private static void RunEffectTest(OpenRgbClient client, Type effectType)
    {
      Console.Clear();
      Console.WriteLine($"=== CONFIGURATION OF: {effectType.Name} ===\n");

      // We instantiate the class dynamically
      var effect = (BaseRgbEffect)Activator.CreateInstance(effectType)!;

      // INTROSPECTION: We retrieve only the properties specific to this effect (DeclaredOnly)
      var properties = effectType.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

      foreach (var prop in properties)
      {
        object? defaultValue = prop.GetValue(effect);
        Console.Write($"- {prop.Name} ({prop.PropertyType.Name}) [Default: {defaultValue}] : ");

        string? userInput = Console.ReadLine();
        if (!string.IsNullOrWhiteSpace(userInput))
        {
          try
          {
            object convertedValue;
            if (prop.PropertyType.IsEnum)
            {
              // Handles enums like AuroraDirection
              convertedValue = Enum.Parse(prop.PropertyType, userInput, true);
            }
            else
            {
              // Handles floats robustly (accepts period or comma)
              string safeInput = userInput.Replace(",", ".");
              convertedValue = Convert.ChangeType(safeInput, prop.PropertyType, CultureInfo.InvariantCulture);
            }
            prop.SetValue(effect, convertedValue);
          }
          catch
          {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  -> Invalid input. Keeping the default value: {defaultValue}");
            Console.ResetColor();
          }
        }
      }

      Console.Write("\n- Device filter regex [Default: .*] : ");
      string? deviceRegexInput = Console.ReadLine();
      string deviceRegex = string.IsNullOrWhiteSpace(deviceRegexInput) ? ".*" : deviceRegexInput!;

      Console.Write("- Zone filter regex [Default: all] : ");
      string? zoneRegexInput = Console.ReadLine();
      string? zoneRegex = string.IsNullOrWhiteSpace(zoneRegexInput) ? null : zoneRegexInput;

      Console.Write("- LED filter regex [Default: all] : ");
      string? ledRegexInput = Console.ReadLine();
      string? ledRegex = string.IsNullOrWhiteSpace(ledRegexInput) ? null : ledRegexInput;

      Console.Write("\n- Simulated sensor value (0-100, or 'auto') [Default: auto] : ");
      string? valInput = Console.ReadLine();
      bool isAutoValue = string.IsNullOrWhiteSpace(valInput) || valInput.Trim().ToLower() == "auto";
      float fixedValue = 100f;
      if (!isAutoValue)
      {
        string safeValue = (valInput ?? string.Empty).Replace(",", ".");
        if (float.TryParse(safeValue, NumberStyles.Float, CultureInfo.InvariantCulture, out float p))
        {
          fixedValue = Math.Clamp(p, 0f, 100f);
        }
      }

      Console.WriteLine("\n▶ Starting render loop... Press [ESC] to stop. (+/- adjust manual value)");
      Console.WriteLine($"  Active filters -> Device: {deviceRegex}, Zone: {(zoneRegex ?? "all")}, LED: {(ledRegex ?? "all")}");

      var devices = client.GetAllControllerData();
      int frameCount = 0;
      DateTime lastManualAdjust = DateTime.MinValue;
      DateTime holdStartTime = DateTime.MinValue;
      ConsoleKey? heldAdjustKey = null;
      bool shouldExit = false;

      Color[][] frameBuffers = new Color[devices.Length][];
      for (int i = 0; i < devices.Length; i++)
      {
        frameBuffers[i] = devices[i].Colors;
      }

      while (true)
      {
        if (Console.KeyAvailable)
        {
          var incoming = Console.ReadKey(true);
          if (incoming.Key == ConsoleKey.Escape)
          {
            break;
          }

          if (incoming.Key == ConsoleKey.Add || incoming.Key == ConsoleKey.OemPlus || incoming.Key == ConsoleKey.Subtract || incoming.Key == ConsoleKey.OemMinus)
          {
            var now = DateTime.UtcNow;
            bool processKey = false;

            if (heldAdjustKey != incoming.Key)
            {
              heldAdjustKey = incoming.Key;
              holdStartTime = now;
              processKey = true;
            }
            else
            {
              var holdDuration = now - holdStartTime;
              int repeatMs = holdDuration > TimeSpan.FromSeconds(1.5)
                  ? 40
                  : holdDuration > TimeSpan.FromSeconds(1.0)
                      ? 60
                      : holdDuration > TimeSpan.FromSeconds(0.5)
                          ? 80
                          : 300;

              if (now - lastManualAdjust >= TimeSpan.FromMilliseconds(repeatMs))
              {
                processKey = true;
              }
            }

            if (processKey && !isAutoValue)
            {
              if (incoming.Key == ConsoleKey.Add || incoming.Key == ConsoleKey.OemPlus)
              {
                fixedValue = Math.Clamp(fixedValue + 1f, 0f, 100f);
              }
              else if (incoming.Key == ConsoleKey.Subtract || incoming.Key == ConsoleKey.OemMinus)
              {
                fixedValue = Math.Clamp(fixedValue - 1f, 0f, 100f);
              }
              Console.Write($"\rMode: manual | Value: {fixedValue:F1}%   ");
            }

            if (processKey)
            {
              lastManualAdjust = now;
            }

            // Remove any repeated identical adjust key events from the buffer.
            while (Console.KeyAvailable)
            {
              var extra = Console.ReadKey(true);
              if (extra.Key != incoming.Key)
              {
                if (extra.Key == ConsoleKey.Escape)
                {
                  shouldExit = true;
                }
                break;
              }
            }
          }
        }

        if (shouldExit)
        {
          break;
        }

        if (heldAdjustKey.HasValue && DateTime.UtcNow - lastManualAdjust > TimeSpan.FromMilliseconds(350))
        {
          heldAdjustKey = null;
        }

        float valToPass = isAutoValue
            ? (50f + 50f * (float)Math.Sin(frameCount * 0.01))
            : fixedValue;

        Console.Write($"\rMode: {(isAutoValue ? "auto  " : "manual")} | Value: {valToPass:F1}%   ");

        // Pass valToPass, and force transitionSpeed to 1.0f for raw testing
        effect.Apply(devices, deviceRegex, zoneRegex, ledRegex, valToPass, frameCount, 1.0f, frameBuffers);

        for (int i = 0; i < devices.Length; i++)
        {
          client.UpdateLeds(i, frameBuffers[i]);
        }

        frameCount++;
        Thread.Sleep(33);
      }

      Console.WriteLine();
    }

    private static void SetAllHardwareColor(OpenRgbClient client, global::OpenRGB.NET.Color color)
    {
      try
      {
        var devices = client.GetAllControllerData();
        for (int i = 0; i < devices.Length; i++)
        {
          // We fill an array with the requested color for each LED of the device
          var colors = Enumerable.Repeat(color, devices[i].Leds.Length).ToArray();
          client.UpdateLeds(i, colors);
        }
      }
      catch { }
    }

  }
}