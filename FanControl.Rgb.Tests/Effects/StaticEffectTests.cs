using FanControl.Rgb.Effects;
using OpenRGB.NET;
using FanControl.Rgb.Tests;
using Xunit;

namespace FanControl.Rgb.Tests.Effects;

public class StaticEffectTests
{
    // MBV=true, value=0 → intensity=0 → black
    [Fact]
    public void ModulateByValue_True_Value0_IsBlack()
    {
        var device = DeviceBuilder.MakeRenderDevice("GPU", 1);
        var buffer = new Color[1];
        var effect = new StaticEffect { ColorHex = "#FFFFFF" };
        effect.Apply([device], "GPU", null, null, 0f, 0, 30, 1f, [buffer]);
        Assert.Equal(0, buffer[0].R);
        Assert.Equal(0, buffer[0].G);
        Assert.Equal(0, buffer[0].B);
    }

    // MBV=true, value=100 → intensity=1 → full color
    [Fact]
    public void ModulateByValue_True_Value100_IsFullColor()
    {
        var device = DeviceBuilder.MakeRenderDevice("GPU", 1);
        var buffer = new Color[1];
        var effect = new StaticEffect { ColorHex = "#FF8040" };
        effect.Apply([device], "GPU", null, null, 100f, 0, 30, 1f, [buffer]);
        Assert.Equal(0xFF, buffer[0].R);
        Assert.Equal(0x80, buffer[0].G);
        Assert.Equal(0x40, buffer[0].B);
    }

    // MBV=false, value=0 → intensity forced to 1 → full color
    [Fact]
    public void ModulateByValue_False_Value0_IsFullColor()
    {
        var device = DeviceBuilder.MakeRenderDevice("GPU", 1);
        var buffer = new Color[1];
        var effect = new StaticEffect { ColorHex = "#FF8040", ModulateByValue = false };
        effect.Apply([device], "GPU", null, null, 0f, 0, 30, 1f, [buffer]);
        Assert.Equal(0xFF, buffer[0].R);
        Assert.Equal(0x80, buffer[0].G);
        Assert.Equal(0x40, buffer[0].B);
    }

    // transitionSpeed in (0,1) exercises the lerp interpolation path in ApplyToTargetLeds.
    // At the reference framerate (30) the fade is applied verbatim (no normalization).
    [Fact]
    public void TransitionSpeed_Half_InterpolatesColor()
    {
        var device = DeviceBuilder.MakeRenderDevice("GPU", 1);
        var buffer = new Color[1]; // starts black
        var effect = new StaticEffect { ColorHex = "#FF0000", ModulateByValue = false };
        effect.Apply([device], "GPU", null, null, 100f, 0, 30, 0.5f, [buffer]);
        // LerpColor(black, red, 0.5) → R = (byte)(0 + 255 * 0.5) = 127
        Assert.Equal(127, buffer[0].R);
        Assert.Equal(0, buffer[0].G);
        Assert.Equal(0, buffer[0].B);
    }

    // The fade is framerate-normalized: a single frame at a lower framerate covers more of
    // the gap toward the target, so the same config smooths over the same wall-clock time.
    [Fact]
    public void TransitionSpeed_LowerFramerate_FadesFasterPerFrame()
    {
        var dev30 = DeviceBuilder.MakeRenderDevice("GPU", 1);
        var buf30 = new Color[1]; // black
        var dev15 = DeviceBuilder.MakeRenderDevice("GPU", 1);
        var buf15 = new Color[1]; // black
        var e30 = new StaticEffect { ColorHex = "#FFFFFF", ModulateByValue = false };
        var e15 = new StaticEffect { ColorHex = "#FFFFFF", ModulateByValue = false };
        e30.Apply([dev30], "GPU", null, null, 100f, 0, 30, 0.1f, [buf30]);
        e15.Apply([dev15], "GPU", null, null, 100f, 0, 15, 0.1f, [buf15]);
        // 30fps → 0.1 verbatim → 25; 15fps → 1-(0.9)^2 = 0.19 → 48.
        Assert.Equal(25, buf30[0].R);
        Assert.True(buf15[0].R > buf30[0].R);
    }
}
