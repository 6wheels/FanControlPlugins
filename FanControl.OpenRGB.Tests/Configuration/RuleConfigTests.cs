using FanControl.OpenRGB;
using Xunit;

namespace FanControl.OpenRGB.Tests.Configuration;

public class RuleConfigTests
{
    [Theory]
    [InlineData(-10f, 0f)]
    [InlineData(0f, 0f)]
    [InlineData(50f, 50f)]
    [InlineData(100f, 100f)]
    [InlineData(110f, 100f)]
    public void ActivationThreshold_Clamped(float input, float expected)
    {
        var config = new RuleConfig { ActivationThreshold = input };
        Assert.Equal(expected, config.ActivationThreshold);
    }

    [Theory]
    [InlineData(0f, 0f)]
    [InlineData(0.5f, 0.5f)]
    [InlineData(1f, 1f)]
    [InlineData(-1f, 0f)]
    [InlineData(2f, 1f)]
    public void TransitionSpeed_Clamped(float input, float expected)
    {
        var config = new RuleConfig { TransitionSpeed = input };
        Assert.Equal(expected, config.TransitionSpeed);
    }

    [Fact]
    public void TransitionSpeed_NullPassthrough()
    {
        var config = new RuleConfig { TransitionSpeed = null };
        Assert.Null(config.TransitionSpeed);
    }

    [Fact]
    public void Defaults_AreCorrect()
    {
        var config = new RuleConfig();
        Assert.Equal(0f, config.ActivationThreshold);
        Assert.Equal(0.1f, config.TransitionSpeed);
        Assert.Equal(".*", config.DeviceRegex);
        Assert.Equal("Animation RGB", config.Name);
    }
}
