using FanControl.Rgb.Toolkit.Rendering;
using FanControl.Rgb;
using FanControl.Rgb.Effects;
using FanControl.Rgb.Rules;
using FanControl.Rgb.Toolkit;
using OpenRGB.NET;
using Xunit;

namespace FanControl.Rgb.Tests.Plugin;

// Covers the pure render helpers (OpenRgbEngine.RenderLayers / RenderStartupFrame).
// Timing / state transitions are covered in OpenRgbEngineTests.
public class RenderFrameTests
{
    static (IRgbDevice[] devices, Color[][] buffers) Setup(params string[] names)
    {
        var devices = names.Select(n => DeviceBuilder.MakeRenderDevice(n, 1)).ToArray();
        var buffers = devices.Select(_ => new Color[1]).ToArray();
        return (devices, buffers);
    }

    static RuleBinding Binding(string deviceRegex, float threshold, float value, BaseRgbEffect? effect = null)
    {
        var config = new RuleConfig
        {
            DeviceRegex = deviceRegex,
            ActivationThreshold = threshold,
            Effect = effect ?? new StaticEffect { ColorHex = "#FF0000", ModulateByValue = false }
        };
        var control = new OpenRgbControlSensor("id", "test");
        control.Set(value);
        return new RuleBinding(config, control);
    }

    static void Render(FakeBroker broker, IRgbDevice[] devices, Color[][] buffers,
        List<RuleBinding> bindings, OpenRgbConfig? config = null)
    {
        var ctx = new RenderContext(broker, devices, buffers, new bool[devices.Length],
            new LayerPriorStore(), bindings, config ?? new OpenRgbConfig());
        OpenRgbEngine.RenderLayers(in ctx, 1);
    }

    // Renders several ticks through a single persistent store, mirroring the real engine
    // (one store reused across frames, incrementing frame counter).
    static void RenderFrames(FakeBroker broker, IRgbDevice[] devices, Color[][] buffers,
        List<RuleBinding> bindings, int frames, OpenRgbConfig? config = null)
    {
        var store = new LayerPriorStore();
        var cfg = config ?? new OpenRgbConfig();
        for (int f = 0; f < frames; f++)
        {
            var ctx = new RenderContext(broker, devices, buffers, new bool[devices.Length],
                store, bindings, cfg);
            OpenRgbEngine.RenderLayers(in ctx, f);
        }
    }

    // Full-featured binding builder: opacity / black-transparency / transition speed.
    static RuleBinding LayerBinding(string deviceRegex, BaseRgbEffect effect,
        float opacity = 1f, bool blackIsTransparent = false, float? transitionSpeed = null, float value = 100f)
    {
        var config = new RuleConfig
        {
            DeviceRegex = deviceRegex,
            ActivationThreshold = 0f,
            Effect = effect,
            Opacity = opacity,
            BlackIsTransparent = blackIsTransparent,
        };
        if (transitionSpeed.HasValue) config.TransitionSpeed = transitionSpeed;
        var control = new OpenRgbControlSensor("id", "test");
        control.Set(value);
        return new RuleBinding(config, control);
    }

    static StaticEffect Static(string hex) => new() { ColorHex = hex, ModulateByValue = false };

    // --- activation threshold ---

    [Fact]
    public void BelowThreshold_NoUpdateLeds()
    {
        var broker = new FakeBroker();
        var (devices, buffers) = Setup("GPU");
        Render(broker, devices, buffers, [Binding("GPU", threshold: 50f, value: 49f)]);
        Assert.Empty(broker.UpdateLedsCalls);
    }

    [Fact]
    public void AtThreshold_UpdateLedsCalled()
    {
        var broker = new FakeBroker();
        var (devices, buffers) = Setup("GPU");
        Render(broker, devices, buffers, [Binding("GPU", threshold: 50f, value: 50f)]);
        Assert.Single(broker.UpdateLedsCalls);
    }

    [Fact]
    public void AboveThreshold_UpdateLedsCalled()
    {
        var broker = new FakeBroker();
        var (devices, buffers) = Setup("GPU");
        Render(broker, devices, buffers, [Binding("GPU", threshold: 50f, value: 75f)]);
        Assert.Single(broker.UpdateLedsCalls);
    }

    // --- value rescaling ---

    [Fact]
    public void ValueRescaling_EffectReceivesNormalizedValue()
    {
        var broker = new FakeBroker();
        var (devices, buffers) = Setup("GPU");
        float? captured = null;
        var effect = new CaptureEffect(v => captured = v);
        // threshold=50, raw=75, range=50 → (75-50)/50*100 = 50
        Render(broker, devices, buffers, [Binding("GPU", threshold: 50f, value: 75f, effect: effect)]);
        Assert.Equal(50f, captured);
    }

    [Fact]
    public void ValueRescaling_AtThreshold_EffectReceivesZero()
    {
        var broker = new FakeBroker();
        var (devices, buffers) = Setup("GPU");
        float? captured = null;
        var effect = new CaptureEffect(v => captured = v);
        // threshold=50, raw=50 → (50-50)/50*100 = 0
        Render(broker, devices, buffers, [Binding("GPU", threshold: 50f, value: 50f, effect: effect)]);
        Assert.Equal(0f, captured);
    }

    [Fact]
    public void ValueRescaling_ZeroThreshold_PassthroughValue()
    {
        var broker = new FakeBroker();
        var (devices, buffers) = Setup("GPU");
        float? captured = null;
        var effect = new CaptureEffect(v => captured = v);
        Render(broker, devices, buffers, [Binding("GPU", threshold: 0f, value: 60f, effect: effect)]);
        Assert.InRange(captured!.Value, 59.99f, 60.01f);
    }

    // --- deviceNeedsUpdate ---

    [Fact]
    public void UnmatchedDevice_NotUpdated()
    {
        var broker = new FakeBroker();
        var (devices, buffers) = Setup("GPU", "CPU");
        Render(broker, devices, buffers, [Binding("GPU", threshold: 0f, value: 100f)]);
        Assert.Single(broker.UpdateLedsCalls);
        Assert.Equal(0, broker.UpdateLedsCalls[0].Index);
    }

    [Fact]
    public void NoBindings_NoUpdateLeds()
    {
        var broker = new FakeBroker();
        var (devices, buffers) = Setup("GPU");
        Render(broker, devices, buffers, []);
        Assert.Empty(broker.UpdateLedsCalls);
    }

    [Fact]
    public void MultipleBindings_EachMatchedDeviceUpdated()
    {
        var broker = new FakeBroker();
        var (devices, buffers) = Setup("GPU", "FAN");
        Render(broker, devices, buffers,
        [
            Binding("GPU", threshold: 0f, value: 100f),
            Binding("FAN", threshold: 0f, value: 100f)
        ]);
        Assert.Equal(2, broker.UpdateLedsCalls.Count);
    }

    // --- startup frame (pushes every device regardless of bindings) ---

    [Fact]
    public void RenderStartupFrame_UpdatesAllDevices()
    {
        var broker = new FakeBroker();
        var (devices, buffers) = Setup("GPU", "FAN");
        var config = new OpenRgbConfig
        {
            Startup = new StartupConfig { DurationSeconds = 60.0, Effect = new StaticEffect { ColorHex = "#0000FF", ModulateByValue = false } }
        };
        var ctx = new RenderContext(broker, devices, buffers, new bool[devices.Length], new LayerPriorStore(), [], config);

        OpenRgbEngine.RenderStartupFrame(in ctx, 1);

        Assert.Equal(2, broker.UpdateLedsCalls.Count);
    }

    // --- layer compositing: config flags propagate through RenderLayers into the writer ---

    // Two opaque=1 layers stacked (blue below, red on top) render full red, not a blend:
    // RenderLayers must pass Opacity=1 through so the top layer fully overwrites (issue #24).
    [Fact]
    public void RenderLayers_OpaqueTopLayer_OverwritesLayerBelow()
    {
        var broker = new FakeBroker();
        var (devices, buffers) = Setup("GPU");
        Render(broker, devices, buffers,
        [
            LayerBinding("GPU", Static("#0000FF"), opacity: 1f, transitionSpeed: 1f),
            LayerBinding("GPU", Static("#FF0000"), opacity: 1f, transitionSpeed: 1f),
        ]);

        Assert.Equal(new Color(255, 0, 0), buffers[0][0]);
    }

    // Opacity=0.5 on the top layer blends with the layer below: proves RenderLayers forwards
    // the per-rule Opacity rather than always overwriting.
    [Fact]
    public void RenderLayers_HalfOpacityTopLayer_BlendsWithLayerBelow()
    {
        var broker = new FakeBroker();
        var (devices, buffers) = Setup("GPU");
        Render(broker, devices, buffers,
        [
            LayerBinding("GPU", Static("#0000FF"), opacity: 1f, transitionSpeed: 1f),
            LayerBinding("GPU", Static("#FF0000"), opacity: 0.5f, transitionSpeed: 1f),
        ]);

        // Lerp(blue, red, 0.5) = (127, 0, 127)
        Assert.Equal(new Color(127, 0, 127), buffers[0][0]);
    }

    // BlackIsTransparent=true forwarded: a black top layer leaves the green layer below.
    [Fact]
    public void RenderLayers_BlackIsTransparent_ShowsLayerBelow()
    {
        var broker = new FakeBroker();
        var (devices, buffers) = Setup("GPU");
        Render(broker, devices, buffers,
        [
            LayerBinding("GPU", Static("#00FF00"), opacity: 1f, transitionSpeed: 1f),
            LayerBinding("GPU", Static("#000000"), opacity: 1f, transitionSpeed: 1f, blackIsTransparent: true),
        ]);

        Assert.Equal(new Color(0, 255, 0), buffers[0][0]);
    }

    // BlackIsTransparent=false (default): black is opaque "off" and overwrites the layer below.
    [Fact]
    public void RenderLayers_BlackOpaqueByDefault_OverwritesLayerBelow()
    {
        var broker = new FakeBroker();
        var (devices, buffers) = Setup("GPU");
        Render(broker, devices, buffers,
        [
            LayerBinding("GPU", Static("#00FF00"), opacity: 1f, transitionSpeed: 1f),
            LayerBinding("GPU", Static("#000000"), opacity: 1f, transitionSpeed: 1f, blackIsTransparent: false),
        ]);

        Assert.Equal(new Color(0, 0, 0), buffers[0][0]);
    }

    // Across ticks, a layer's temporal smoothing fades from its OWN prior (persisted in the
    // per-sink store), never leaking the blue layer below. After two frames of a 0.5 fade the
    // top red has climbed 127 → 191 with zero blue bleed — proving both prior persistence and
    // the temporal/layer decoupling at the engine level.
    [Fact]
    public void RenderLayers_TemporalSmoothing_PersistsAndDoesNotLeakLowerLayer()
    {
        var broker = new FakeBroker();
        var (devices, buffers) = Setup("GPU");
        RenderFrames(broker, devices, buffers,
        [
            LayerBinding("GPU", Static("#0000FF"), opacity: 1f, transitionSpeed: 1f),   // opaque blue below
            LayerBinding("GPU", Static("#FF0000"), opacity: 1f, transitionSpeed: 0.5f), // red fading in on top
        ], frames: 2);

        Color result = buffers[0][0];
        Assert.Equal(191, result.R); // 127 (frame 0) → 191 (frame 1): prior persisted across ticks
        Assert.Equal(0, result.G);
        Assert.Equal(0, result.B);   // no blue bleed from the layer below
    }
}

internal sealed class CaptureEffect(Action<float> capture) : BaseRgbEffect
{
    protected override void ProcessEffect(IRgbDevice device, string? zoneRegex, string? ledRegex, float value, int frameCount, int framerate, float transitionSpeed, LedWriter writer)
        => capture(value);
}
