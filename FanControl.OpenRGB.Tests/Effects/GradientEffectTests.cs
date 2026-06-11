using FanControl.OpenRGB.Effects;
using OpenRGB.NET;
using FanControl.OpenRGB.Tests;
using Xunit;

namespace FanControl.OpenRGB.Tests.Effects;

// GradientEffect does not check ModulateByValue — value maps directly to color position.
public class GradientEffectTests
{
    [Fact]
    public void Value0_IsColorMin()
    {
        var device = DeviceBuilder.MakeDevice("GPU", 1);
        var buffer = new Color[1];
        var effect = new GradientEffect { ColorMinHex = "#00FF00", ColorMaxHex = "#FF0000" };
        effect.Apply([device], "GPU", null, null, 0f, 0, 1f, [buffer]);
        Assert.Equal(0x00, buffer[0].R);
        Assert.Equal(0xFF, buffer[0].G);
        Assert.Equal(0x00, buffer[0].B);
    }

    [Fact]
    public void Value100_IsColorMax()
    {
        var device = DeviceBuilder.MakeDevice("GPU", 1);
        var buffer = new Color[1];
        var effect = new GradientEffect { ColorMinHex = "#00FF00", ColorMaxHex = "#FF0000" };
        effect.Apply([device], "GPU", null, null, 100f, 0, 1f, [buffer]);
        Assert.Equal(0xFF, buffer[0].R);
        Assert.Equal(0x00, buffer[0].G);
        Assert.Equal(0x00, buffer[0].B);
    }

    [Fact]
    public void Value50_IsMidpoint()
    {
        var device = DeviceBuilder.MakeDevice("GPU", 1);
        var buffer = new Color[1];
        // #000000 → #FF0000: at ratio=0.5, R = (byte)(0 + 255 * 0.5) = 127
        var effect = new GradientEffect { ColorMinHex = "#000000", ColorMaxHex = "#FF0000" };
        effect.Apply([device], "GPU", null, null, 50f, 0, 1f, [buffer]);
        Assert.Equal(127, buffer[0].R);
        Assert.Equal(0, buffer[0].G);
        Assert.Equal(0, buffer[0].B);
    }
}
