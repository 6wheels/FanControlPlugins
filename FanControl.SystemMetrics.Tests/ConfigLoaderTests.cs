using FanControl.SystemMetrics.Tests.Plugin;
using FanControl.SystemMetrics.Toolkit;
using Xunit;

namespace FanControl.SystemMetrics.Tests;

public class ConfigLoaderTests
{
    [Fact]
    public void Parse_ReadsEnabledMetrics()
    {
        var config = ConfigLoader.Parse("""{ "EnabledMetrics": ["CpuLoad", "DiskTime"] }""");
        Assert.Equal(["CpuLoad", "DiskTime"], config.EnabledMetrics);
    }

    [Fact]
    public void Parse_NullPayload_ReturnsDefault()
    {
        var config = ConfigLoader.Parse("null");
        Assert.Equal(MetricCatalog.All.Select(d => d.Key), config.EnabledMetrics);
    }

    [Fact]
    public void SerializeTemplate_RoundTripsAllCatalogKeys()
    {
        var config = ConfigLoader.Parse(ConfigLoader.SerializeTemplate());
        Assert.Equal(MetricCatalog.All.Select(d => d.Key), config.EnabledMetrics);
    }

    [Fact]
    public void LoadOrCreate_MissingFile_WritesTemplateAndReturnsNull()
    {
        var dialog = new FakeDialog();
        var log = new List<string>();
        string path = Path.Combine(Path.GetTempPath(), $"sm_{Guid.NewGuid():N}.json");
        try
        {
            var config = ConfigLoader.LoadOrCreate(path, dialog, log.Add);

            Assert.Null(config);
            Assert.True(File.Exists(path));
            Assert.Single(dialog.Messages);
            // The written template must itself be valid and enable all metrics.
            var template = ConfigLoader.Parse(File.ReadAllText(path));
            Assert.Equal(MetricCatalog.All.Select(d => d.Key), template.EnabledMetrics);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void LoadOrCreate_ExistingFile_ParsesIt()
    {
        var dialog = new FakeDialog();
        string path = Path.Combine(Path.GetTempPath(), $"sm_{Guid.NewGuid():N}.json");
        File.WriteAllText(path, """{ "EnabledMetrics": ["GpuLoad"] }""");
        try
        {
            var config = ConfigLoader.LoadOrCreate(path, dialog, _ => { });

            Assert.NotNull(config);
            Assert.Equal(["GpuLoad"], config!.EnabledMetrics);
            Assert.Empty(dialog.Messages);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void LoadOrCreate_MalformedJson_ReturnsDefaultAndLogs()
    {
        var dialog = new FakeDialog();
        var log = new List<string>();
        string path = Path.Combine(Path.GetTempPath(), $"sm_{Guid.NewGuid():N}.json");
        File.WriteAllText(path, "{ not json");
        try
        {
            var config = ConfigLoader.LoadOrCreate(path, dialog, log.Add);

            Assert.NotNull(config);
            Assert.Equal(MetricCatalog.All.Select(d => d.Key), config!.EnabledMetrics);
            Assert.Contains(log, m => m.Contains("failed", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            File.Delete(path);
        }
    }
}
