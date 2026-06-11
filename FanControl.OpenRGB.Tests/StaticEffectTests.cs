using FanControl.OpenRGB.Effects;
using OpenRGB.NET;
using Xunit;

namespace FanControl.OpenRGB.Tests;

public class StaticEffectTests
{
    // MBV=true, value=0 → intensity=0 → black
    [Fact]
    public void ModulateByValue_True_Value0_IsBlack()
    {
        var device = DeviceBuilder.MakeDevice("GPU", 1);
        var buffer = new Color[1];
        var effect = new StaticEffect { ColorHex = "#FFFFFF" };
        effect.Apply([device], "GPU", null, null, 0f, 0, 1f, [buffer]);
        Assert.Equal(0, buffer[0].R);
        Assert.Equal(0, buffer[0].G);
        Assert.Equal(0, buffer[0].B);
    }

    // MBV=true, value=100 → intensity=1 → full color
    [Fact]
    public void ModulateByValue_True_Value100_IsFullColor()
    {
        var device = DeviceBuilder.MakeDevice("GPU", 1);
        var buffer = new Color[1];
        var effect = new StaticEffect { ColorHex = "#FF8040" };
        effect.Apply([device], "GPU", null, null, 100f, 0, 1f, [buffer]);
        Assert.Equal(0xFF, buffer[0].R);
        Assert.Equal(0x80, buffer[0].G);
        Assert.Equal(0x40, buffer[0].B);
    }

    // MBV=false, value=0 → intensity forced to 1 → full color
    [Fact]
    public void ModulateByValue_False_Value0_IsFullColor()
    {
        var device = DeviceBuilder.MakeDevice("GPU", 1);
        var buffer = new Color[1];
        var effect = new StaticEffect { ColorHex = "#FF8040", ModulateByValue = false };
        effect.Apply([device], "GPU", null, null, 0f, 0, 1f, [buffer]);
        Assert.Equal(0xFF, buffer[0].R);
        Assert.Equal(0x80, buffer[0].G);
        Assert.Equal(0x40, buffer[0].B);
    }

    // transitionSpeed in (0,1) exercises the lerp interpolation path in ApplyToTargetLeds
    [Fact]
    public void TransitionSpeed_Half_InterpolatesColor()
    {
        var device = DeviceBuilder.MakeDevice("GPU", 1);
        var buffer = new Color[1]; // starts black
        var effect = new StaticEffect { ColorHex = "#FF0000", ModulateByValue = false };
        effect.Apply([device], "GPU", null, null, 100f, 0, 0.5f, [buffer]);
        // LerpColor(black, red, 0.5) → R = (byte)(0 + 255 * 0.5) = 127
        Assert.Equal(127, buffer[0].R);
        Assert.Equal(0, buffer[0].G);
        Assert.Equal(0, buffer[0].B);
    }
}
