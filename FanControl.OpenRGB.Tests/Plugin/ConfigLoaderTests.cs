using FanControl.OpenRGB;
using FanControl.OpenRGB.Toolkit;
using Xunit;

namespace FanControl.OpenRGB.Tests.Plugin;

public class ConfigLoaderTests
{
    [Fact]
    public void Parse_ReadsRulesAndSettings()
    {
        var config = ConfigLoader.Parse("""
        {
            "ServerPort": 1234,
            "Framerate": 60,
            "Rules": [ { "Name": "GPU", "DeviceRegex": "GPU" } ]
        }
        """);

        Assert.Equal(1234, config.ServerPort);
        Assert.Equal(60, config.Framerate);
        Assert.Single(config.Rules);
        Assert.Equal("GPU", config.Rules[0].DeviceRegex);
    }

    [Fact]
    public void Parse_AllowsCommentsAndTrailingCommas()
    {
        var config = ConfigLoader.Parse("""
        {
            // server framerate
            "Framerate": 30,
        }
        """);

        Assert.Equal(30, config.Framerate);
    }

    [Fact]
    public void Parse_ClampsOutOfRangeFramerate()
    {
        Assert.Equal(1, ConfigLoader.Parse("""{ "Framerate": 0 }""").Framerate);
        Assert.Equal(240, ConfigLoader.Parse("""{ "Framerate": 9999 }""").Framerate);
    }

    [Fact]
    public void Parse_EmptyObject_YieldsDefaults()
    {
        var config = ConfigLoader.Parse("{}");
        Assert.Equal(30, config.Framerate);
        Assert.Equal(6742, config.ServerPort);
        Assert.Empty(config.Rules);
    }

    [Fact]
    public void Parse_Malformed_Throws()
    {
        Assert.ThrowsAny<Exception>(() => ConfigLoader.Parse("{ not json"));
    }

    [Fact]
    public void Parse_DefaultReconnect_WhenOmitted()
    {
        var config = ConfigLoader.Parse("{}");
        Assert.Equal(5, config.Reconnect.MaxRetries);
        Assert.Equal(5.0, config.Reconnect.DelaySeconds);
    }

    [Fact]
    public void Parse_ReadsReconnectSettings()
    {
        var config = ConfigLoader.Parse("""{ "Reconnect": { "MaxRetries": 3, "DelaySeconds": 2.5 } }""");
        Assert.Equal(3, config.Reconnect.MaxRetries);
        Assert.Equal(2.5, config.Reconnect.DelaySeconds);
    }

    [Fact]
    public void Parse_ClampsNegativeReconnectValues()
    {
        var config = ConfigLoader.Parse("""{ "Reconnect": { "MaxRetries": -1, "DelaySeconds": -10 } }""");
        Assert.Equal(0, config.Reconnect.MaxRetries);
        Assert.Equal(0.0, config.Reconnect.DelaySeconds);
    }

    [Fact]
    public void SerializeTemplate_RoundTripsToDefaults()
    {
        var config = ConfigLoader.Parse(ConfigLoader.SerializeTemplate());
        Assert.Equal(6742, config.ServerPort);
        Assert.Equal(30, config.Framerate);
        Assert.Empty(config.Rules);
    }

    // --- LoadOrCreate (disk orchestration) ---

    [Fact]
    public void LoadOrCreate_MissingFile_WritesTemplate_ReturnsNull_ShowsDialog()
    {
        using var temp = new TempFile();
        var dialog = new FakeDialog();
        var logged = new List<string>();

        var result = ConfigLoader.LoadOrCreate(temp.Path, dialog, (m, _) => logged.Add(m));

        Assert.Null(result); // caller aborts init
        Assert.True(File.Exists(temp.Path));
        Assert.Single(dialog.Messages);
        Assert.Contains(logged, m => m.Contains("Template"));
        // Written template is valid and parses to defaults.
        Assert.Equal(30, ConfigLoader.Parse(File.ReadAllText(temp.Path)).Framerate);
    }

    [Fact]
    public void LoadOrCreate_ExistingValidFile_ReturnsParsedConfig()
    {
        using var temp = new TempFile();
        File.WriteAllText(temp.Path, """{ "ServerPort": 9999, "Framerate": 45 }""");
        var dialog = new FakeDialog();

        var result = ConfigLoader.LoadOrCreate(temp.Path, dialog, (_, _) => { });

        Assert.NotNull(result);
        Assert.Equal(9999, result!.ServerPort);
        Assert.Equal(45, result.Framerate);
        Assert.Empty(dialog.Messages); // no template dialog for an existing file
    }

    [Fact]
    public void LoadOrCreate_MalformedFile_ReturnsDefaults_LogsError()
    {
        using var temp = new TempFile();
        File.WriteAllText(temp.Path, "{ not json");
        var logged = new List<(string Msg, LogLevel Level)>();

        var result = ConfigLoader.LoadOrCreate(temp.Path, new FakeDialog(), (m, l) => logged.Add((m, l)));

        Assert.NotNull(result); // degrades to defaults instead of dying
        Assert.Equal(30, result!.Framerate);
        Assert.Contains(logged, e => e.Level == LogLevel.Error);
    }

    private sealed class TempFile : IDisposable
    {
        public string Path { get; } =
            System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"openrgb_cfg_{Guid.NewGuid():N}.json");

        public void Dispose()
        {
            try { if (File.Exists(Path)) File.Delete(Path); } catch { }
        }
    }
}
