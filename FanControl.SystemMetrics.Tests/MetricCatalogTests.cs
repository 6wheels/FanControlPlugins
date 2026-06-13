using Xunit;

namespace FanControl.SystemMetrics.Tests;

public class MetricCatalogTests
{
    [Fact]
    public void All_ContainsNineMetrics()
    {
        Assert.Equal(9, MetricCatalog.All.Count);
    }

    [Fact]
    public void All_KeysAreUnique()
    {
        var keys = MetricCatalog.All.Select(d => d.Key).ToList();
        Assert.Equal(keys.Count, keys.Distinct(StringComparer.OrdinalIgnoreCase).Count());
    }

    [Fact]
    public void All_IdsAreUnique()
    {
        var ids = MetricCatalog.All.Select(d => d.Id).ToList();
        Assert.Equal(ids.Count, ids.Distinct().Count());
    }

    [Theory]
    [InlineData("NetworkIO")]
    [InlineData("DiskRead")]
    [InlineData("DiskWrite")]
    [InlineData("RamUsage")]
    [InlineData("CpuFreq")]
    [InlineData("PageFile")]
    public void All_ContainsNewMetric(string key)
    {
        Assert.Contains(MetricCatalog.All, d => d.Key == key);
    }
}
