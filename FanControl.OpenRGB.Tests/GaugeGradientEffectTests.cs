using FanControl.OpenRGB.Effects;
using OpenRGB.NET;
using Xunit;

namespace FanControl.OpenRGB.Tests;

public class GaugeGradientEffectTests
{
    // MBV=true, value=100 → valueRatio=1 → fillPosition=4 → all 4 inside fill → weight=1 → ColorMax
    [Fact]
    public void ModulateByValue_True_Value100_AllColorMax()
    {
        var device = DeviceBuilder.MakeDevice("GPU", 4);
        var buffer = new Color[4];
        var effect = new GaugeGradientEffect { ColorMinHex = "#000000", ColorMaxHex = "#FF0000" };
        effect.Apply([device], "GPU", null, null, 100f, 0, 1f, [buffer]);
        Assert.All(buffer, c => Assert.Equal(0xFF, c.R));
    }

    // MBV=false, value=0 → forced valueRatio=1 → same as value=100 with MBV=true
    [Fact]
    public void ModulateByValue_False_Value0_AllColorMax()
    {
        var device = DeviceBuilder.MakeDevice("GPU", 4);
        var buffer = new Color[4];
        var effect = new GaugeGradientEffect { ColorMinHex = "#000000", ColorMaxHex = "#FF0000", ModulateByValue = false };
        effect.Apply([device], "GPU", null, null, 0f, 0, 1f, [buffer]);
        Assert.All(buffer, c => Assert.Equal(0xFF, c.R));
    }

    // 2D matrix path
    [Fact]
    public void MatrixDevice_Value100_AllColorMax()
    {
        var device = DeviceBuilder.MakeMatrixDevice("GPU", 4, 2);
        var buffer = new Color[8];
        var effect = new GaugeGradientEffect { ColorMinHex = "#000000", ColorMaxHex = "#FF0000" };
        effect.Apply([device], "GPU", null, null, 100f, 0, 1f, [buffer]);
        Assert.All(buffer, c => Assert.Equal(0xFF, c.R));
    }

    // MBV=true, value=0 → valueRatio=0 → fillPosition=0 → soft glow at boundary
    // 4 LEDs: edgeWidth = clamp(4/4, 2, 5) = 2
    // LED 0: distance=0.5, weight = clamp(1 - 0.5/2, 0, 1) = 0.75 → partial brightness
    [Fact]
    public void ModulateByValue_True_Value0_GlowAtBoundary()
    {
        var device = DeviceBuilder.MakeDevice("GPU", 4);
        var buffer = new Color[4];
        var effect = new GaugeGradientEffect { ColorMinHex = "#000000", ColorMaxHex = "#FF0000" };
        effect.Apply([device], "GPU", null, null, 0f, 0, 1f, [buffer]);
        Assert.True(buffer[0].R > 0, "LED 0 should glow (boundary effect)");
        Assert.True(buffer[0].R < 0xFF, "LED 0 should not be at full brightness");
    }
}
