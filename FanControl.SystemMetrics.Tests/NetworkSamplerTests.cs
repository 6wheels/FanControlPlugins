using FanControl.SystemMetrics.Sampling;
using Xunit;

namespace FanControl.SystemMetrics.Tests;

public class NetworkSamplerTests
{
    [Fact]
    public void ToPercent_FullLinkSaturation_Returns100()
    {
        // 1000 Mbps = 125_000_000 bytes/sec fully saturates the link.
        Assert.Equal(100f, NetworkSampler.ToPercent(125_000_000f, 1000));
    }

    [Fact]
    public void ToPercent_HalfLink_Returns50()
    {
        Assert.Equal(50f, NetworkSampler.ToPercent(62_500_000f, 1000));
    }

    [Fact]
    public void ToPercent_Idle_ReturnsZero()
    {
        Assert.Equal(0f, NetworkSampler.ToPercent(0f, 1000));
    }

    [Fact]
    public void ToPercent_OverCapacity_ClampsTo100()
    {
        Assert.Equal(100f, NetworkSampler.ToPercent(500_000_000f, 1000));
    }

    [Fact]
    public void ToPercent_NonPositiveLinkSpeed_ReturnsZero()
    {
        Assert.Equal(0f, NetworkSampler.ToPercent(125_000_000f, 0));
    }
}
