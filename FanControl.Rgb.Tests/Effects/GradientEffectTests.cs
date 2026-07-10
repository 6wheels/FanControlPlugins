using FanControl.Rgb.Effects;
using OpenRGB.NET;
using FanControl.Rgb.Tests;
using Xunit;

namespace FanControl.Rgb.Tests.Effects;

// GradientEffect does not check ModulateByValue — value maps directly to color position.
public class GradientEffectTests
{
    [Fact]
    public void Value0_IsColorMin()
    {
        var device = DeviceBuilder.MakeRenderDevice("GPU", 1);
        var buffer = new Color[1];
        var effect = new GradientEffect { ColorMinHex = "#00FF00", ColorMaxHex = "#FF0000" };
        effect.Apply([device], "GPU", null, null, 0f, 0, 30, 1f, [buffer]);
        Assert.Equal(0x00, buffer[0].R);
        Assert.Equal(0xFF, buffer[0].G);
        Assert.Equal(0x00, buffer[0].B);
    }

    [Fact]
    public void Value100_IsColorMax()
    {
        var device = DeviceBuilder.MakeRenderDevice("GPU", 1);
        var buffer = new Color[1];
        var effect = new GradientEffect { ColorMinHex = "#00FF00", ColorMaxHex = "#FF0000" };
        effect.Apply([device], "GPU", null, null, 100f, 0, 30, 1f, [buffer]);
        Assert.Equal(0xFF, buffer[0].R);
        Assert.Equal(0x00, buffer[0].G);
        Assert.Equal(0x00, buffer[0].B);
    }

    [Fact]
    public void Value50_IsMidpoint()
    {
        var device = DeviceBuilder.MakeRenderDevice("GPU", 1);
        var buffer = new Color[1];
        // #000000 → #FF0000: at ratio=0.5, R = (byte)(0 + 255 * 0.5) = 127
        var effect = new GradientEffect { ColorMinHex = "#000000", ColorMaxHex = "#FF0000" };
        effect.Apply([device], "GPU", null, null, 50f, 0, 30, 1f, [buffer]);
        Assert.Equal(127, buffer[0].R);
        Assert.Equal(0, buffer[0].G);
        Assert.Equal(0, buffer[0].B);
    }

    // 3-stop gradient: value=50 lands exactly on the middle stop (#FFFF00).
    [Fact]
    public void ColorStops_ThreeStops_Value50_IsMiddleStop()
    {
        var device = DeviceBuilder.MakeRenderDevice("GPU", 1);
        var buffer = new Color[1];
        var effect = new GradientEffect
        {
            ColorStops =
            [
                new GradientStop { Position = 0.0f, ColorHex = "#00FF00" },
                new GradientStop { Position = 0.5f, ColorHex = "#FFFF00" },
                new GradientStop { Position = 1.0f, ColorHex = "#FF0000" },
            ]
        };
        effect.Apply([device], "GPU", null, null, 50f, 0, 30, 1f, [buffer]);
        Assert.Equal(0xFF, buffer[0].R);
        Assert.Equal(0xFF, buffer[0].G);
        Assert.Equal(0x00, buffer[0].B);
    }

    // ColorStops overrides ColorMinHex/ColorMaxHex when present.
    [Fact]
    public void ColorStops_OverrideMinMax()
    {
        var device = DeviceBuilder.MakeRenderDevice("GPU", 1);
        var buffer = new Color[1];
        var effect = new GradientEffect
        {
            ColorMinHex = "#000000",
            ColorMaxHex = "#000000",
            ColorStops =
            [
                new GradientStop { Position = 0.0f, ColorHex = "#0000FF" },
                new GradientStop { Position = 1.0f, ColorHex = "#0000FF" },
            ]
        };
        effect.Apply([device], "GPU", null, null, 50f, 0, 30, 1f, [buffer]);
        // Fallback min/max would yield black; the stops force blue.
        Assert.Equal(0x00, buffer[0].R);
        Assert.Equal(0x00, buffer[0].G);
        Assert.Equal(0xFF, buffer[0].B);
    }

    // Empty ColorStops falls back to ColorMinHex/ColorMaxHex (back-compat).
    [Fact]
    public void EmptyColorStops_FallsBackToMinMax()
    {
        var device = DeviceBuilder.MakeRenderDevice("GPU", 1);
        var buffer = new Color[1];
        var effect = new GradientEffect { ColorMinHex = "#00FF00", ColorMaxHex = "#FF0000" };
        effect.Apply([device], "GPU", null, null, 0f, 0, 30, 1f, [buffer]);
        Assert.Equal(0x00, buffer[0].R);
        Assert.Equal(0xFF, buffer[0].G);
    }
}
