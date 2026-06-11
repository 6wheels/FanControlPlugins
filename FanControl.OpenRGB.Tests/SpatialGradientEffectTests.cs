using FanControl.OpenRGB.Effects;
using OpenRGB.NET;
using Xunit;

namespace FanControl.OpenRGB.Tests;

public class SpatialGradientEffectTests
{
    // 1D zone: first LED (position 0) → ratio=0 → ColorMin
    [Fact]
    public void FirstLed_IsColorMin()
    {
        var device = DeviceBuilder.MakeDevice("GPU", 4);
        var buffer = new Color[4];
        var effect = new SpatialGradientEffect { ColorMinHex = "#00FF00", ColorMaxHex = "#0000FF", ModulateByValue = false };
        effect.Apply([device], "GPU", null, null, 0f, 0, 1f, [buffer]);
        Assert.Equal(0x00, buffer[0].R);
        Assert.Equal(0xFF, buffer[0].G);
        Assert.Equal(0x00, buffer[0].B);
    }

    // 1D zone: last LED (position N-1) → ratio=1 → ColorMax
    [Fact]
    public void LastLed_IsColorMax()
    {
        var device = DeviceBuilder.MakeDevice("GPU", 4);
        var buffer = new Color[4];
        var effect = new SpatialGradientEffect { ColorMinHex = "#00FF00", ColorMaxHex = "#0000FF", ModulateByValue = false };
        effect.Apply([device], "GPU", null, null, 0f, 0, 1f, [buffer]);
        Assert.Equal(0x00, buffer[3].R);
        Assert.Equal(0x00, buffer[3].G);
        Assert.Equal(0xFF, buffer[3].B);
    }

    // MBV=true, value=0 → intensity=0 → all black
    [Fact]
    public void ModulateByValue_True_Value0_IsBlack()
    {
        var device = DeviceBuilder.MakeDevice("GPU", 4);
        var buffer = new Color[4];
        var effect = new SpatialGradientEffect { ModulateByValue = true };
        effect.Apply([device], "GPU", null, null, 0f, 0, 1f, [buffer]);
        Assert.All(buffer, c => Assert.Equal(0, c.R + c.G + c.B));
    }

    // MBV=false, value=0 → intensity forced to 1 → colors visible
    [Fact]
    public void ModulateByValue_False_Value0_ColorsVisible()
    {
        var device = DeviceBuilder.MakeDevice("GPU", 4);
        var buffer = new Color[4];
        var effect = new SpatialGradientEffect { ColorMinHex = "#00FF00", ColorMaxHex = "#0000FF", ModulateByValue = false };
        effect.Apply([device], "GPU", null, null, 0f, 0, 1f, [buffer]);
        Assert.Contains(buffer, c => c.R + c.G + c.B > 0);
    }

    // 2D matrix path: column position drives ratio (x / (width-1))
    [Fact]
    public void MatrixDevice_FirstColumn_IsColorMin_LastColumn_IsColorMax()
    {
        var device = DeviceBuilder.MakeMatrixDevice("GPU", 4, 2); // 4 wide, 2 tall, 8 LEDs
        var buffer = new Color[8];
        var effect = new SpatialGradientEffect { ColorMinHex = "#00FF00", ColorMaxHex = "#0000FF", ModulateByValue = false };
        effect.Apply([device], "GPU", null, null, 0f, 0, 1f, [buffer]);
        // LED (y=0, x=0) → index 0 → ratio=0 → ColorMin (#00FF00)
        Assert.Equal(0x00, buffer[0].R);
        Assert.Equal(0xFF, buffer[0].G);
        Assert.Equal(0x00, buffer[0].B);
        // LED (y=0, x=3) → index 3 → ratio=1 → ColorMax (#0000FF)
        Assert.Equal(0x00, buffer[3].R);
        Assert.Equal(0x00, buffer[3].G);
        Assert.Equal(0xFF, buffer[3].B);
    }
}
