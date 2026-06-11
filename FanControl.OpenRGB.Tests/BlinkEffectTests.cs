using FanControl.OpenRGB.Effects;
using OpenRGB.NET;
using Xunit;

namespace FanControl.OpenRGB.Tests;

public class BlinkEffectTests
{
    static void Apply(BlinkEffect effect, Device device, Color[] buffer, float value, int frameCount)
        => effect.Apply([device], "GPU", null, null, value, frameCount, 1f, [buffer]);

    // Default: Max=30, Min=2. value=0 → ratio=0, interval=30, period=60.
    // frame=0: phase=0 < 30 → Color1
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

    // value=0, interval=30. frame=30: phase=30, not < 30 → Color2
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

    // value=100 → ratio=1, interval=2, period=4. frame=0: phase=0 < 2 → Color1
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

    // value=100, interval=2. frame=2: phase=2, not < 2 → Color2
    [Fact]
    public void ModulateByValue_True_Value100_Frame2_IsColor2()
    {
        var device = DeviceBuilder.MakeDevice("GPU", 1);
        var buffer = new Color[1];
        var effect = new BlinkEffect { Color1Hex = "#FF0000", Color2Hex = "#0000FF" };
        Apply(effect, device, buffer, 100f, 2);
        Assert.Equal(0x00, buffer[0].R);
        Assert.Equal(0xFF, buffer[0].B);
    }

    // MBV=false forces ratio=1 regardless of value → interval=2. frame=2, value=0 → Color2
    [Fact]
    public void ModulateByValue_False_Value0_Frame2_IsColor2()
    {
        var device = DeviceBuilder.MakeDevice("GPU", 1);
        var buffer = new Color[1];
        var effect = new BlinkEffect { Color1Hex = "#FF0000", Color2Hex = "#0000FF", ModulateByValue = false };
        Apply(effect, device, buffer, 0f, 2);
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
    public void MinBlinkIntervalFrames_ClampedTo1()
    {
        var effect = new BlinkEffect { MinBlinkIntervalFrames = 0 };
        Assert.Equal(1, effect.MinBlinkIntervalFrames);
    }

    [Fact]
    public void MaxBlinkIntervalFrames_ClampedTo30()
    {
        var effect = new BlinkEffect { MaxBlinkIntervalFrames = 99 };
        Assert.Equal(30, effect.MaxBlinkIntervalFrames);
    }

    // Exercises the BlinkIntervalFrames getter and setter (separate from Min/Max properties)
    [Fact]
    public void BlinkIntervalFrames_ClampedTo1()
    {
        var effect = new BlinkEffect { BlinkIntervalFrames = 0 };
        Assert.Equal(1, effect.BlinkIntervalFrames);
    }
}
