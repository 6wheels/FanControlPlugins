using FanControl.Rgb.Effects;
using OpenRGB.NET;
using FanControl.Rgb.Tests;
using Xunit;

namespace FanControl.Rgb.Tests.Effects;

public class AuroraEffectTests
{
    // MBV=true, value=0 → intensity=0 → all black
    [Fact]
    public void ModulateByValue_True_Value0_IsBlack()
    {
        var device = DeviceBuilder.MakeRenderDevice("GPU", 4);
        var buffer = new Color[4];
        var effect = new AuroraEffect { ModulateByValue = true };
        effect.Apply([device], "GPU", null, null, 0f, 0, 30, 1f, [buffer]);
        Assert.All(buffer, c => Assert.Equal(0, c.R + c.G + c.B));
    }

    // MBV=true, value=100 → intensity=1 → non-black
    // At frame=0, 1D LED 0: factor=0.5, color = c2 (#00FFFF) → (0,255,255)
    [Fact]
    public void ModulateByValue_True_Value100_NonBlack()
    {
        var device = DeviceBuilder.MakeRenderDevice("GPU", 4);
        var buffer = new Color[4];
        var effect = new AuroraEffect { ModulateByValue = true };
        effect.Apply([device], "GPU", null, null, 100f, 0, 30, 1f, [buffer]);
        Assert.Contains(buffer, c => c.R + c.G + c.B > 0);
    }

    // MBV=false, value=0 → intensity forced to 1 → non-black
    [Fact]
    public void ModulateByValue_False_Value0_NonBlack()
    {
        var device = DeviceBuilder.MakeRenderDevice("GPU", 4);
        var buffer = new Color[4];
        var effect = new AuroraEffect { ModulateByValue = false };
        effect.Apply([device], "GPU", null, null, 0f, 0, 30, 1f, [buffer]);
        Assert.Contains(buffer, c => c.R + c.G + c.B > 0);
    }

    // deviceRegex no match → buffer untouched
    [Fact]
    public void DeviceRegex_NoMatch_BufferUntouched()
    {
        var device = DeviceBuilder.MakeRenderDevice("GPU", 1);
        var buffer = new Color[] { new Color(0x11, 0x22, 0x33) };
        new AuroraEffect().Apply([device], "CPU", null, null, 100f, 1, 30, 1f, [buffer]);
        Assert.Equal(0x11, buffer[0].R);
    }

    // 2D matrix path
    [Fact]
    public void MatrixDevice_Value100_NonBlack()
    {
        var device = DeviceBuilder.MakeRenderMatrixDevice("GPU", 5, 3);
        var buffer = new Color[15];
        var effect = new AuroraEffect { ModulateByValue = false };
        effect.Apply([device], "GPU", null, null, 0f, 1, 30, 1f, [buffer]);
        Assert.Contains(buffer, c => c.R + c.G + c.B > 0);
    }

    // Vertical direction exercises the isVertical branch in CalculateAuroraColor
    [Fact]
    public void VerticalDirection_MatrixDevice_NonBlack()
    {
        var device = DeviceBuilder.MakeRenderMatrixDevice("GPU", 5, 3);
        var buffer = new Color[15];
        var effect = new AuroraEffect { Direction = AuroraDirection.Vertical, ModulateByValue = false };
        effect.Apply([device], "GPU", null, null, 0f, 1, 30, 1f, [buffer]);
        Assert.Contains(buffer, c => c.R + c.G + c.B > 0);
    }

    // Saturation=0 desaturates the whole palette → every lit cell is grayscale (R==G==B).
    [Fact]
    public void Saturation_Zero_IsGrayscale()
    {
        var device = DeviceBuilder.MakeRenderDevice("GPU", 4);
        var buffer = new Color[4];
        var effect = new AuroraEffect { Saturation = 0f, ModulateByValue = false };
        effect.Apply([device], "GPU", null, null, 0f, 0, 30, 1f, [buffer]);
        Assert.All(buffer, c =>
        {
            Assert.Equal(c.R, c.G);
            Assert.Equal(c.G, c.B);
        });
        Assert.Contains(buffer, c => c.R + c.G + c.B > 0);
    }

    // Saturation is clamped to [0,1].
    [Fact]
    public void Saturation_ClampedToUnitRange()
    {
        var effect = new AuroraEffect { Saturation = 5f };
        Assert.Equal(1f, effect.Saturation);
        effect.Saturation = -1f;
        Assert.Equal(0f, effect.Saturation);
    }
}
