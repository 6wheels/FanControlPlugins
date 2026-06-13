using System.Text.Json;
using FanControl.Plugins;

namespace FanControl.SystemMetrics.Toolkit;

internal static class ConfigLoader
{
    private static readonly JsonSerializerOptions ReadOptions = new()
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    private static readonly JsonSerializerOptions WriteOptions = new() { WriteIndented = true };

    // Pure parse: throws on malformed JSON, defaults on a null payload. Testable without IO.
    public static SystemMetricsConfig Parse(string json) =>
        JsonSerializer.Deserialize<SystemMetricsConfig>(json, ReadOptions) ?? new SystemMetricsConfig();

    public static string SerializeTemplate() =>
        JsonSerializer.Serialize(new SystemMetricsConfig(), WriteOptions);

    // Loads config from disk. If the file is absent, writes a template and returns null
    // (caller should register nothing). On parse error, returns a default config to keep
    // the host alive rather than leaving the plugin dead.
    public static SystemMetricsConfig? LoadOrCreate(string path, IPluginDialog dialog, Action<string> log)
    {
        if (!File.Exists(path))
        {
            File.WriteAllText(path, SerializeTemplate());
            log("Template configuration file generated. Please configure it.");
            dialog.ShowMessageDialog("SystemMetrics: Template configuration file generated. Please configure it.");
            return null;
        }

        try
        {
            var config = Parse(File.ReadAllText(path));
            log($"Configuration loaded. {config.EnabledMetrics.Count} metric(s) enabled.");
            return config;
        }
        catch (Exception ex)
        {
            log($"Configuration failed: {ex.Message}");
            return new SystemMetricsConfig();
        }
    }
}
