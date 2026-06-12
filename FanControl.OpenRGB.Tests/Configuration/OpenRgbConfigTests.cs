using FanControl.OpenRGB;
using FanControl.OpenRGB.Effects;
using Xunit;

namespace FanControl.OpenRGB.Tests.Configuration;

public class OpenRgbConfigTests
{
    [Fact]
    public void Defaults_AreCorrect()
    {
        var config = new OpenRgbConfig();
        Assert.Equal("127.0.0.1", config.ServerIp);
        Assert.Equal(6742, config.ServerPort);
        Assert.Equal(30, config.Framerate);
        Assert.Equal(0.1f, config.TransitionSpeed);
        Assert.Equal(LogLevel.Info, config.LogLevel);
        Assert.Null(config.Startup);
        Assert.Empty(config.Rules);
    }
}

public class StartupConfigTests
{
    [Fact]
    public void Defaults_AreCorrect()
    {
        var config = new StartupConfig { Effect = new StaticEffect() };
        Assert.Equal(5.0, config.DurationSeconds);
        Assert.NotNull(config.Effect);
    }
}
