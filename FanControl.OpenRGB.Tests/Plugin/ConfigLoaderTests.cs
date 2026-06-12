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
}
