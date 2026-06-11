using FanControl.OpenRGB.Toolkit;
using Xunit;

namespace FanControl.OpenRGB.Tests.Toolkit;

public class AutoValueDriverTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(314)]
    [InlineData(628)]
    [InlineData(10000)]
    public void Compute_AlwaysInRange(int frame)
    {
        float value = AutoValueDriver.Compute(frame);

        Assert.InRange(value, 0f, 100f);
    }

    [Fact]
    public void Compute_Frame0_Returns50()
    {
        // sin(0) = 0, so 50 + 50*0 = 50
        Assert.Equal(50f, AutoValueDriver.Compute(0));
    }

    [Fact]
    public void Compute_HalfPeriod_IsApprox50()
    {
        // sin(π) ≈ 0, frame = π/0.01 ≈ 314
        float value = AutoValueDriver.Compute(314);

        Assert.InRange(value, 49f, 51f);
    }
}
