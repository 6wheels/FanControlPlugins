using FanControl.OpenRGB.Effects;
using OpenRGB.NET;
using FanControl.OpenRGB.Tests;
using Xunit;

namespace FanControl.OpenRGB.Tests.Effects;

public class BreathingEffectTests
{
    // Formula: sine = (sin(frameCount*speed) + 1) / 2. At frame=0: sine=0.5 → midpoint between base and peak.
    [Fact]
    public void Frame0_IsMidpointColor()
    {
        var device = DeviceBuilder.MakeDevice("GPU", 1);
        var buffer = new Color[1];
        var effect = new BreathingEffect { BaseColorHex = "#000000", PeakColorHex = "#0000FF" };
        effect.Apply([device], "GPU", null, null, 50f, 0, 1f, [buffer]);
        // B = (byte)(0 + (255-0) * 0.5) = 127
        Assert.Equal(0, buffer[0].R);
        Assert.Equal(0, buffer[0].G);
        Assert.Equal(127, buffer[0].B);
    }

    // MBV=true: different values produce different speeds → different colors at same frame
    // value=0: speed=MinSpeed=0.02, frame=100 → sin(2) ≈ 0.909
    // value=100: speed=MaxSpeed=0.15, frame=100 → sin(15) ≈ 0.650
    [Fact]
    public void ModulateByValue_True_DifferentValues_GiveDifferentColors()
    {
        var dev0 = DeviceBuilder.MakeDevice("GPU", 1);
        var buf0 = new Color[1];
        var effectLow = new BreathingEffect { BaseColorHex = "#000000", PeakColorHex = "#0000FF", ModulateByValue = true };
        effectLow.Apply([dev0], "GPU", null, null, 0f, 100, 1f, [buf0]);

        var dev100 = DeviceBuilder.MakeDevice("GPU", 1);
        var buf100 = new Color[1];
        var effectHigh = new BreathingEffect { BaseColorHex = "#000000", PeakColorHex = "#0000FF", ModulateByValue = true };
        effectHigh.Apply([dev100], "GPU", null, null, 100f, 100, 1f, [buf100]);

        Assert.NotEqual(buf0[0].B, buf100[0].B);
    }

    // MBV=false forces ratio=1 → speed=MaxSpeed. Should match MBV=true at value=100.
    [Fact]
    public void ModulateByValue_False_Value0_MatchesMaxSpeed()
    {
        var devFalse = DeviceBuilder.MakeDevice("GPU", 1);
        var bufFalse = new Color[1];
        var effectFalse = new BreathingEffect { BaseColorHex = "#000000", PeakColorHex = "#0000FF", ModulateByValue = false };
        effectFalse.Apply([devFalse], "GPU", null, null, 0f, 100, 1f, [bufFalse]);

        var devTrue = DeviceBuilder.MakeDevice("GPU", 1);
        var bufTrue = new Color[1];
        var effectTrue = new BreathingEffect { BaseColorHex = "#000000", PeakColorHex = "#0000FF", ModulateByValue = true };
        effectTrue.Apply([devTrue], "GPU", null, null, 100f, 100, 1f, [bufTrue]);

        Assert.Equal(bufTrue[0].B, bufFalse[0].B);
    }
}
