using FanControl.Rgb.Effects;
using OpenRGB.NET;
using FanControl.Rgb.Tests;
using Xunit;

namespace FanControl.Rgb.Tests.Effects;

// ProgressBarEffect does not check ModulateByValue — value maps directly to fill ratio.
public class ProgressBarEffectTests
{
    // value=0 with transparent empty → fill=0, buffer untouched
    [Fact]
    public void Value0_TransparentEmpty_BufferUntouched()
    {
        var device = DeviceBuilder.MakeRenderDevice("GPU", 4);
        var buffer = new Color[4];
        buffer[0] = new Color(0x11, 0x11, 0x11);
        var effect = new ProgressBarEffect { FillColorHex = "#FF0000", EmptyColorHex = "Transparent" };
        effect.Apply([device], "GPU", null, null, 0f, 0, 30, 1f, [buffer]);
        Assert.Equal(0x11, buffer[0].R);
    }

    // value=100 → all 4 LEDs filled
    [Fact]
    public void Value100_AllLedsFilled()
    {
        var device = DeviceBuilder.MakeRenderDevice("GPU", 4);
        var buffer = new Color[4];
        var effect = new ProgressBarEffect { FillColorHex = "#FF0000", EmptyColorHex = "Transparent" };
        effect.Apply([device], "GPU", null, null, 100f, 0, 30, 1f, [buffer]);
        Assert.All(buffer, c => Assert.Equal(0xFF, c.R));
    }

    // value=50 → fillCount = Round(0.5 * 4) = 2 → first 2 filled, last 2 untouched
    [Fact]
    public void Value50_HalfFilled()
    {
        var device = DeviceBuilder.MakeRenderDevice("GPU", 4);
        var buffer = new Color[4];
        var effect = new ProgressBarEffect { FillColorHex = "#FF0000", EmptyColorHex = "Transparent" };
        effect.Apply([device], "GPU", null, null, 50f, 0, 30, 1f, [buffer]);
        Assert.Equal(0xFF, buffer[0].R);
        Assert.Equal(0xFF, buffer[1].R);
        Assert.Equal(0x00, buffer[2].R);
        Assert.Equal(0x00, buffer[3].R);
    }

    // Non-transparent empty color → unfilled LEDs painted with empty color
    [Fact]
    public void NonTransparentEmpty_UnfilledLedsGetEmptyColor()
    {
        var device = DeviceBuilder.MakeRenderDevice("GPU", 4);
        var buffer = new Color[4];
        var effect = new ProgressBarEffect { FillColorHex = "#FF0000", EmptyColorHex = "#0000FF" };
        effect.Apply([device], "GPU", null, null, 50f, 0, 30, 1f, [buffer]);
        Assert.Equal(0xFF, buffer[0].R); // filled
        Assert.Equal(0xFF, buffer[1].R); // filled
        Assert.Equal(0xFF, buffer[2].B); // unfilled → empty color
        Assert.Equal(0xFF, buffer[3].B); // unfilled → empty color
    }

    // 2D matrix path
    [Fact]
    public void MatrixDevice_Value50_HalfFilled()
    {
        var device = DeviceBuilder.MakeRenderMatrixDevice("GPU", 4, 2); // 8 LEDs total
        var buffer = new Color[8];
        var effect = new ProgressBarEffect { FillColorHex = "#FF0000", EmptyColorHex = "Transparent" };
        effect.Apply([device], "GPU", null, null, 50f, 0, 30, 1f, [buffer]);
        // fillCount = Round(0.5 * 8) = 4
        Assert.Equal(4, buffer.Count(c => c.R == 0xFF));
    }

    // Reverse: value=50 on 4 LEDs → the last 2 fill (2,3), first 2 empty.
    [Fact]
    public void Reverse_Value50_FillsLastHalf()
    {
        var device = DeviceBuilder.MakeRenderDevice("GPU", 4);
        var buffer = new Color[4];
        var effect = new ProgressBarEffect
        {
            FillColorHex = "#FF0000",
            EmptyColorHex = "#0000FF",
            FillDirection = FillDirection.Reverse
        };
        effect.Apply([device], "GPU", null, null, 50f, 0, 30, 1f, [buffer]);
        Assert.Equal(0xFF, buffer[0].B); // empty
        Assert.Equal(0xFF, buffer[1].B); // empty
        Assert.Equal(0xFF, buffer[2].R); // filled
        Assert.Equal(0xFF, buffer[3].R); // filled
    }

    // CenterOut: value=50 on 4 LEDs → centerStart=(4-2)/2=1 → LEDs 1,2 fill.
    [Fact]
    public void CenterOut_Value50_FillsMiddle()
    {
        var device = DeviceBuilder.MakeRenderDevice("GPU", 4);
        var buffer = new Color[4];
        var effect = new ProgressBarEffect
        {
            FillColorHex = "#FF0000",
            EmptyColorHex = "#0000FF",
            FillDirection = FillDirection.CenterOut
        };
        effect.Apply([device], "GPU", null, null, 50f, 0, 30, 1f, [buffer]);
        Assert.Equal(0xFF, buffer[0].B); // empty
        Assert.Equal(0xFF, buffer[1].R); // filled
        Assert.Equal(0xFF, buffer[2].R); // filled
        Assert.Equal(0xFF, buffer[3].B); // empty
    }

    // FillDirection round-trips through JSON polymorphic deserialization.
    [Fact]
    public void FillDirection_JsonRoundTrips()
    {
        var json = """{"Type":"ProgressBar","FillDirection":"CenterOut"}""";
        var effect = System.Text.Json.JsonSerializer.Deserialize<BaseRgbEffect>(json);
        var bar = Assert.IsType<ProgressBarEffect>(effect);
        Assert.Equal(FillDirection.CenterOut, bar.FillDirection);
    }
}
