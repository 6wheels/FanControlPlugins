using FanControl.Rgb.Effects;
using FanControl.Rgb.Tests;
using FanControl.Rgb.Toolkit.Rendering;
using OpenRGB.NET;
using Xunit;

namespace FanControl.Rgb.Tests.Toolkit;

public class LedWriterTests
{
    private static readonly Color Red = new(255, 0, 0);
    private static readonly Color Blue = new(0, 0, 255);
    private static readonly Color Green = new(0, 255, 0);
    private static readonly Color Black = new(0, 0, 0);

    private static void AssertColor(Color expected, Color actual)
    {
        Assert.Equal(expected.R, actual.R);
        Assert.Equal(expected.G, actual.G);
        Assert.Equal(expected.B, actual.B);
    }

    // Opacity=1 (full overwrite) over a filled layer below renders the top color, not a blend.
    [Fact]
    public void Opacity1_OverLayerBelow_FullOverwrite()
    {
        var below = new[] { Blue };
        var prior = new Color[1];
        var writer = new LedWriter(below, prior, opacity: 1f);

        writer.Write(0, Red, fade: 1f);

        AssertColor(Red, below[0]);
    }

    // Opacity between 0 and 1 blends predictably with the layer below.
    [Fact]
    public void OpacityHalf_BlendsWithLayerBelow()
    {
        var below = new[] { Blue };
        var prior = new Color[1];
        var writer = new LedWriter(below, prior, opacity: 0.5f);

        writer.Write(0, Red, fade: 1f);

        // Lerp(blue, red, 0.5) = (127, 0, 127)
        AssertColor(new Color(127, 0, 127), below[0]);
    }

    // Temporal smoothing fades from THIS layer's own prior output, not from the buffer below.
    // Prior already holds red, so a half-fade write of red stays red even though blue sits below.
    [Fact]
    public void TransitionSpeed_SmoothsFromOwnPrior_NoLowerLayerLeak()
    {
        var below = new[] { Blue };
        var prior = new[] { Red };
        var writer = new LedWriter(below, prior, opacity: 1f);

        writer.Write(0, Red, fade: 0.5f);

        AssertColor(Red, below[0]);
    }

    // Legacy path (no dedicated prior): temporal smoothing reads the buffer itself, so a
    // half-fade over blue bleeds into purple — the exact behavior the prior buffer removes.
    [Fact]
    public void NoPrior_SmoothsFromBuffer_LegacyBleed()
    {
        var below = new[] { Blue };
        var writer = new LedWriter(below); // prior == buffer

        writer.Write(0, Red, fade: 0.5f);

        AssertColor(new Color(127, 0, 127), below[0]);
    }

    // BlackIsTransparent=true: a black output skips the composite and leaves the layer below.
    [Fact]
    public void BlackIsTransparent_True_SkipsComposite()
    {
        var below = new[] { Green };
        var prior = new Color[1];
        var writer = new LedWriter(below, prior, opacity: 1f, blackIsTransparent: true);

        writer.Write(0, Black, fade: 1f);

        AssertColor(Green, below[0]);
    }

    // BlackIsTransparent=false (default): black is an opaque "off" color and overwrites.
    [Fact]
    public void BlackIsTransparent_False_BlackIsOpaque()
    {
        var below = new[] { Green };
        var prior = new Color[1];
        var writer = new LedWriter(below, prior, opacity: 1f, blackIsTransparent: false);

        writer.Write(0, Black, fade: 1f);

        AssertColor(Black, below[0]);
    }

    // End-to-end through Apply: red opaque layer over a blue buffer renders full red (issue #24).
    [Fact]
    public void Apply_RedOpaqueOverBlue_RendersFullRed()
    {
        var device = DeviceBuilder.MakeRenderDevice("GPU", 1);
        var below = new[] { Blue };
        var prior = new Color[1];
        var effect = new StaticEffect { ColorHex = "#FF0000", ModulateByValue = false };

        effect.Apply([device], "GPU", null, null, 100f, 0, 30, 1f, [below], [prior], opacity: 1f);

        AssertColor(Red, below[0]);
    }

    // End-to-end: a gauge whose empty region outputs black shows the layer below when
    // BlackIsTransparent is on (GaugeGradient bypasses ApplyToTargetLeds — proves the
    // centralized writer reaches the direct-write effects too).
    [Fact]
    public void Apply_GaugeBlackIsTransparent_ShowsLayerBelow()
    {
        var device = DeviceBuilder.MakeRenderDevice("GPU", 4);
        var below = new[] { Green, Green, Green, Green };
        var prior = new Color[4];
        // value=0 → empty gauge → low region samples ColorMin (#000000) → black.
        var effect = new GaugeGradientEffect { ColorMinHex = "#000000", ColorMaxHex = "#FF0000" };

        effect.Apply([device], "GPU", null, null, 0f, 0, 30, 1f, [below], [prior], opacity: 1f, blackIsTransparent: true);

        // Far LEDs sit past the soft glow → pure black → transparent → keep the green below.
        // (Near LEDs fall inside the glow ramp, so they are non-black and get painted.)
        AssertColor(Green, below[2]);
        AssertColor(Green, below[3]);
    }
}
