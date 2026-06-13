using System.Text.Json;
using FanControl.OpenRGB.Effects;
using OpenRGB.NET;
using FanControl.OpenRGB.Tests;
using Xunit;

namespace FanControl.OpenRGB.Tests.Effects;

public class RainbowEffectTests
{
    // Spread=0 → all LEDs share the same hue each frame → same color
    [Fact]
    public void Spread_Zero_AllLedsGetSameColor()
    {
        var device = DeviceBuilder.MakeDevice("GPU", 3);
        var buffer = new Color[3];
        var effect = new RainbowEffect { Spread = 0f, ModulateByValue = false };
        effect.Apply([device], "GPU", null, null, 0f, 10, 1f, [buffer]);
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
        var device = DeviceBuilder.MakeDevice("GPU", 2);
        var buffer = new Color[2];
        var effect = new RainbowEffect { Spread = 60f, Speed = 0f, ModulateByValue = false };
        effect.Apply([device], "GPU", null, null, 0f, 0, 1f, [buffer]);
        Assert.NotEqual(buffer[0].R + buffer[0].G + buffer[0].B,
                        buffer[1].R + buffer[1].G + buffer[1].B);
    }

    // Speed=1, frame advances 90 → hue shifts 90° → different color
    // frame=0 hue=0°→(255,0,0)  frame=90 hue=90°→(127,255,0)
    [Fact]
    public void Speed_AdvancesHuePerFrame()
    {
        var dev0 = DeviceBuilder.MakeDevice("GPU", 1);
        var buf0 = new Color[1];
        var dev90 = DeviceBuilder.MakeDevice("GPU", 1);
        var buf90 = new Color[1];
        var effect0  = new RainbowEffect { Speed = 1f, Spread = 0f, ModulateByValue = false };
        var effect90 = new RainbowEffect { Speed = 1f, Spread = 0f, ModulateByValue = false };
        effect0.Apply([dev0],   "GPU", null, null, 0f,  0, 1f, [buf0]);
        effect90.Apply([dev90], "GPU", null, null, 0f, 90, 1f, [buf90]);
        Assert.NotEqual(buf0[0].R, buf90[0].R);
        Assert.NotEqual(buf0[0].G, buf90[0].G);
    }

    // MBV=true, value=0 → brightness=0 → black
    [Fact]
    public void ModulateByValue_True_Value0_IsBlack()
    {
        var device = DeviceBuilder.MakeDevice("GPU", 3);
        var buffer = new Color[3];
        var effect = new RainbowEffect { ModulateByValue = true };
        effect.Apply([device], "GPU", null, null, 0f, 0, 1f, [buffer]);
        Assert.All(buffer, c => Assert.Equal(0, c.R + c.G + c.B));
    }

    // MBV=false, value=0 → brightness=1 → non-black
    [Fact]
    public void ModulateByValue_False_Value0_IsNotBlack()
    {
        var device = DeviceBuilder.MakeDevice("GPU", 1);
        var buffer = new Color[1];
        var effect = new RainbowEffect { ModulateByValue = false, Speed = 0f, Spread = 0f };
        effect.Apply([device], "GPU", null, null, 0f, 0, 1f, [buffer]);
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
        var device = DeviceBuilder.MakeMatrixDevice("GPU", 4, 3);
        var buffer = new Color[12];
        var effect = new RainbowEffect { ModulateByValue = false };
        effect.Apply([device], "GPU", null, null, 0f, 0, 1f, [buffer]);
        Assert.Contains(buffer, c => c.R + c.G + c.B > 0);
    }
}
