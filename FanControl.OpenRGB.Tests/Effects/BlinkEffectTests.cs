using FanControl.OpenRGB.Effects;
using OpenRGB.NET;
using FanControl.OpenRGB.Tests;
using Xunit;

namespace FanControl.OpenRGB.Tests.Effects;

public class BlinkEffectTests
{
    static void Apply(BlinkEffect effect, Device device, Color[] buffer, float value, int frameCount)
        => effect.Apply([device], "GPU", null, null, value, frameCount, 1f, [buffer]);

    // Default: SlowBlinkHz=0.5, FastBlinkHz=15, Framerate=30.
    // value=0 → hz=0.5, framesPerHalf=30, period=60. frame=0 → Color1.
    [Fact]
    public void ModulateByValue_True_Value0_Frame0_IsColor1()
    {
        var device = DeviceBuilder.MakeDevice("GPU", 1);
        var buffer = new Color[1];
        var effect = new BlinkEffect { Color1Hex = "#FF0000", Color2Hex = "#0000FF" };
        Apply(effect, device, buffer, 0f, 0);
        Assert.Equal(0xFF, buffer[0].R);
        Assert.Equal(0x00, buffer[0].B);
    }

    // value=0, framesPerHalf=30. frame=30: phase=30 not < 30 → Color2.
    [Fact]
    public void ModulateByValue_True_Value0_Frame30_IsColor2()
    {
        var device = DeviceBuilder.MakeDevice("GPU", 1);
        var buffer = new Color[1];
        var effect = new BlinkEffect { Color1Hex = "#FF0000", Color2Hex = "#0000FF" };
        Apply(effect, device, buffer, 0f, 30);
        Assert.Equal(0x00, buffer[0].R);
        Assert.Equal(0xFF, buffer[0].B);
    }

    // value=100 → hz=15, framesPerHalf=1, period=2. frame=0 → Color1.
    [Fact]
    public void ModulateByValue_True_Value100_Frame0_IsColor1()
    {
        var device = DeviceBuilder.MakeDevice("GPU", 1);
        var buffer = new Color[1];
        var effect = new BlinkEffect { Color1Hex = "#FF0000", Color2Hex = "#0000FF" };
        Apply(effect, device, buffer, 100f, 0);
        Assert.Equal(0xFF, buffer[0].R);
        Assert.Equal(0x00, buffer[0].B);
    }

    // value=100, framesPerHalf=1. frame=1: phase=1 not < 1 → Color2.
    [Fact]
    public void ModulateByValue_True_Value100_Frame1_IsColor2()
    {
        var device = DeviceBuilder.MakeDevice("GPU", 1);
        var buffer = new Color[1];
        var effect = new BlinkEffect { Color1Hex = "#FF0000", Color2Hex = "#0000FF" };
        Apply(effect, device, buffer, 100f, 1);
        Assert.Equal(0x00, buffer[0].R);
        Assert.Equal(0xFF, buffer[0].B);
    }

    // MBV=false forces ratio=1 → hz=FastBlinkHz=15, framesPerHalf=1. frame=1 → Color2.
    [Fact]
    public void ModulateByValue_False_Value0_Frame1_IsColor2()
    {
        var device = DeviceBuilder.MakeDevice("GPU", 1);
        var buffer = new Color[1];
        var effect = new BlinkEffect { Color1Hex = "#FF0000", Color2Hex = "#0000FF", ModulateByValue = false };
        Apply(effect, device, buffer, 0f, 1);
        Assert.Equal(0x00, buffer[0].R);
        Assert.Equal(0xFF, buffer[0].B);
    }

    // deviceRegex no match → buffer untouched
    [Fact]
    public void DeviceRegex_NoMatch_BufferUntouched()
    {
        var device = DeviceBuilder.MakeDevice("GPU", 1);
        var buffer = new Color[] { new Color(0x11, 0x22, 0x33) };
        new BlinkEffect().Apply([device], "CPU", null, null, 0f, 0, 1f, [buffer]);
        Assert.Equal(0x11, buffer[0].R);
    }

    [Fact]
    public void SlowBlinkHz_ClampedToPoint1()
    {
        var effect = new BlinkEffect { SlowBlinkHz = 0f };
        Assert.Equal(0.1f, effect.SlowBlinkHz);
    }

    [Fact]
    public void FastBlinkHz_ClampedToPoint1()
    {
        var effect = new BlinkEffect { FastBlinkHz = 0f };
        Assert.Equal(0.1f, effect.FastBlinkHz);
    }

}
