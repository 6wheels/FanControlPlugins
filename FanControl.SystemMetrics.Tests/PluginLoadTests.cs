using FanControl.SystemMetrics.Tests.Plugin;
using Xunit;

namespace FanControl.SystemMetrics.Tests;

public class PluginLoadTests
{
    private static MetricsPlugin Build(SystemMetricsConfig config) =>
        new(new FakeDialog(), new FakeLogger(), config);

    [Fact]
    public void Load_RegistersOnlyEnabledMetrics()
    {
        var plugin = Build(new SystemMetricsConfig { EnabledMetrics = ["CpuLoad", "DiskTime"] });
        var container = new FakeContainer();

        plugin.Load(container);

        Assert.Equal(2, container.TempSensors.Count);
        Assert.Contains(container.TempSensors, s => s.Id == "SYS_CPU_LOAD");
        Assert.Contains(container.TempSensors, s => s.Id == "SYS_DISK_TIME");
        Assert.DoesNotContain(container.TempSensors, s => s.Id == "SYS_GPU_LOAD");
    }

    [Fact]
    public void Load_EmptyList_RegistersNothing()
    {
        var plugin = Build(new SystemMetricsConfig { EnabledMetrics = [] });
        var container = new FakeContainer();

        plugin.Load(container);

        Assert.Empty(container.TempSensors);
    }

    [Fact]
    public void Load_FullList_RegistersAllCatalogMetrics()
    {
        var plugin = Build(new SystemMetricsConfig());
        var container = new FakeContainer();

        plugin.Load(container);

        Assert.Equal(MetricCatalog.All.Count, container.TempSensors.Count);
    }

    [Fact]
    public void Load_KeyMatchIsCaseInsensitive()
    {
        var plugin = Build(new SystemMetricsConfig { EnabledMetrics = ["cpuload"] });
        var container = new FakeContainer();

        plugin.Load(container);

        Assert.Single(container.TempSensors);
        Assert.Equal("SYS_CPU_LOAD", container.TempSensors[0].Id);
    }
}
