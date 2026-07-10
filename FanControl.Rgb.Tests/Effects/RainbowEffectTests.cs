using System.Text.Json;
using FanControl.Rgb.Effects;
using OpenRGB.NET;
using FanControl.Rgb.Tests;
using Xunit;

namespace FanControl.Rgb.Tests.Effects;

public class RainbowEffectTests
{
    // Spread=0 → all LEDs share the same hue each frame → same color
    [Fact]
    public void Spread_Zero_AllLedsGetSameColor()
    {
        var device = DeviceBuilder.MakeRenderDevice("GPU", 3);
        var buffer = new Color[3];
        var effect = new RainbowEffect { Spread = 0f, ModulateByValue = false };
        effect.Apply([device], "GPU", null, null, 0f, 10, 30, 1f, [buffer]);
        Assert.Equal(buffer[0].R, buffer[1].R);
        Assert.Equal(buffer[0].G, buffer[1].G);
        Assert.Equal(buffer[0].B, buffer[1].B);
        Assert.Equal(buffer[1].R, buffer[2].R);
        Assert.Equal(buffer[1].G, buffer[2].G);
        Assert.Equal(buffer[1].B, buffer[2].B);
    }

    // Spread=60 → adjacent LEDs 60° apart → different colors
    // LED 0: hue=0° → (255,0,0)   LED 1: hue=60° → (255,255,0)
    [Fact]
    public void Spread_NonZero_AdjacentLedsGetDifferentColors()
    {
        var device = DeviceBuilder.MakeRenderDevice("GPU", 2);
        var buffer = new Color[2];
        var effect = new RainbowEffect { Spread = 60f, Speed = 0f, ModulateByValue = false };
        effect.Apply([device], "GPU", null, null, 0f, 0, 30, 1f, [buffer]);
        Assert.NotEqual(buffer[0].R + buffer[0].G + buffer[0].B,
                        buffer[1].R + buffer[1].G + buffer[1].B);
    }

    // Speed=1.0 ≈ one cycle every 4s (reference rate). At framerate=30, frame 60 = 2.0s =
    // half a cycle → hue 180°. frame=0 hue=0°→(255,0,0)   frame=60 hue=180°→(0,255,255)
    [Fact]
    public void Speed_AdvancesHueOverTime()
    {
        var dev0 = DeviceBuilder.MakeRenderDevice("GPU", 1);
        var buf0 = new Color[1];
        var dev60 = DeviceBuilder.MakeRenderDevice("GPU", 1);
        var buf60 = new Color[1];
        var effect0  = new RainbowEffect { Speed = 1f, Spread = 0f, ModulateByValue = false };
        var effect60 = new RainbowEffect { Speed = 1f, Spread = 0f, ModulateByValue = false };
        effect0.Apply([dev0],   "GPU", null, null, 0f,  0, 30, 1f, [buf0]);
        effect60.Apply([dev60], "GPU", null, null, 0f, 60, 30, 1f, [buf60]);
        Assert.NotEqual(buf0[0].R, buf60[0].R);
        Assert.NotEqual(buf0[0].G, buf60[0].G);
    }

    // MBV=true, value=0 → brightness=0 → black
    [Fact]
    public void ModulateByValue_True_Value0_IsBlack()
    {
        var device = DeviceBuilder.MakeRenderDevice("GPU", 3);
        var buffer = new Color[3];
        var effect = new RainbowEffect { ModulateByValue = true };
        effect.Apply([device], "GPU", null, null, 0f, 0, 30, 1f, [buffer]);
        Assert.All(buffer, c => Assert.Equal(0, c.R + c.G + c.B));
    }

    // MBV=false, value=0 → brightness=1 → non-black
    [Fact]
    public void ModulateByValue_False_Value0_IsNotBlack()
    {
        var device = DeviceBuilder.MakeRenderDevice("GPU", 1);
        var buffer = new Color[1];
        var effect = new RainbowEffect { ModulateByValue = false, Speed = 0f, Spread = 0f };
        effect.Apply([device], "GPU", null, null, 0f, 0, 30, 1f, [buffer]);
        // hue=0° at full brightness → R=255
        Assert.Equal(255, buffer[0].R);
    }

    // "Rainbow" discriminator must round-trip through JSON polymorphic deserialization
    [Fact]
    public void JsonDiscriminator_RoundTrips()
    {
        var json = """{"Type":"Rainbow","Speed":2.0,"Spread":0.5}""";
        var effect = JsonSerializer.Deserialize<BaseRgbEffect>(json);
        var rainbow = Assert.IsType<RainbowEffect>(effect);
        Assert.Equal(2.0f, rainbow.Speed);
        Assert.Equal(0.5f, rainbow.Spread);
    }

    // Matrix device path: all cells get non-black at full brightness
    [Fact]
    public void MatrixDevice_NonBlack()
    {
        var device = DeviceBuilder.MakeRenderMatrixDevice("GPU", 4, 3);
        var buffer = new Color[12];
        var effect = new RainbowEffect { ModulateByValue = false };
        effect.Apply([device], "GPU", null, null, 0f, 0, 30, 1f, [buffer]);
        Assert.Contains(buffer, c => c.R + c.G + c.B > 0);
    }

    // Saturation=0 collapses the hue to grayscale → R==G==B.
    [Fact]
    public void Saturation_Zero_IsGrayscale()
    {
        var device = DeviceBuilder.MakeRenderDevice("GPU", 1);
        var buffer = new Color[1];
        var effect = new RainbowEffect { Saturation = 0f, Speed = 0f, Spread = 0f, ModulateByValue = false };
        effect.Apply([device], "GPU", null, null, 0f, 0, 30, 1f, [buffer]);
        Assert.Equal(buffer[0].R, buffer[0].G);
        Assert.Equal(buffer[0].G, buffer[0].B);
        Assert.Equal(255, buffer[0].R); // full brightness, no color
    }

    // StartHue offsets the base hue: 120° → green.
    [Fact]
    public void StartHue_OffsetsBaseHue()
    {
        var device = DeviceBuilder.MakeRenderDevice("GPU", 1);
        var buffer = new Color[1];
        var effect = new RainbowEffect { StartHue = 120f, Speed = 0f, Spread = 0f, ModulateByValue = false };
        effect.Apply([device], "GPU", null, null, 0f, 0, 30, 1f, [buffer]);
        Assert.Equal(0x00, buffer[0].R);
        Assert.Equal(0xFF, buffer[0].G);
        Assert.Equal(0x00, buffer[0].B);
    }

    // Speed is wall-clock: the same elapsed second yields the same hue at any framerate.
    // 0.5s = frame 15 @ 30fps = frame 30 @ 60fps → identical color.
    [Fact]
    public void Speed_IsFramerateIndependent()
    {
        var dev30 = DeviceBuilder.MakeRenderDevice("GPU", 1);
        var buf30 = new Color[1];
        var dev60 = DeviceBuilder.MakeRenderDevice("GPU", 1);
        var buf60 = new Color[1];
        var e30 = new RainbowEffect { Speed = 1f, Spread = 0f, ModulateByValue = false };
        var e60 = new RainbowEffect { Speed = 1f, Spread = 0f, ModulateByValue = false };
        e30.Apply([dev30], "GPU", null, null, 0f, 15, 30, 1f, [buf30]);
        e60.Apply([dev60], "GPU", null, null, 0f, 30, 60, 1f, [buf60]);
        Assert.Equal(buf30[0].R, buf60[0].R);
        Assert.Equal(buf30[0].G, buf60[0].G);
        Assert.Equal(buf30[0].B, buf60[0].B);
    }
}
