using System.Text.Json;
using FanControl.Plugins;

namespace FanControl.OpenRGB.Toolkit;

internal static class ConfigLoader
{
    private static readonly JsonSerializerOptions ReadOptions = new()
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    private static readonly JsonSerializerOptions WriteOptions = new() { WriteIndented = true };

    // Pure parse: throws on malformed JSON, defaults on a null payload. Testable without IO.
    public static OpenRgbConfig Parse(string json) =>
        JsonSerializer.Deserialize<OpenRgbConfig>(json, ReadOptions) ?? new OpenRgbConfig();

    public static string SerializeTemplate() =>
        JsonSerializer.Serialize(new OpenRgbConfig(), WriteOptions);

    // Loads config from disk. If the file is absent, writes a template and returns null
    // (caller should abort init). On parse error, returns a default config to keep the
    // host alive rather than leaving the plugin dead.
    public static OpenRgbConfig? LoadOrCreate(string path, IPluginDialog dialog, Action<string, LogLevel> log)
    {
        if (!File.Exists(path))
        {
            File.WriteAllText(path, SerializeTemplate());
            log("Template configuration file generated. Please configure it.", LogLevel.Info);
            dialog.ShowMessageDialog("OpenRGB: Template configuration file generated. Please configure it.");
            return null;
        }

        try
        {
            var config = Parse(File.ReadAllText(path));
            log($"Configuration loaded. {config.Rules.Count} rule(s) found.", LogLevel.Info);
            log($"Parsed configuration dump:\n{JsonSerializer.Serialize(config, WriteOptions)}", LogLevel.Debug);
            log($"Server IP: {config.ServerIp}, Port: {config.ServerPort}, Framerate: {config.Framerate} FPS, LogLevel: {config.LogLevel}", LogLevel.Debug);
            return config;
        }
        catch (Exception ex)
        {
            log($"Configuration failed: {ex.Message}", LogLevel.Error);
            return new OpenRgbConfig();
        }
    }
}
