using FanControl.OpenRGB;
using FanControl.OpenRGB.Effects;
using FanControl.OpenRGB.Rules;
using FanControl.OpenRGB.Toolkit;
using OpenRGB.NET;
using Xunit;

namespace FanControl.OpenRGB.Tests.Plugin;

// Covers the pure render helpers (OpenRgbEngine.RenderLayers / RenderStartupFrame).
// Timing / state transitions are covered in OpenRgbEngineTests.
public class RenderFrameTests
{
    static (Device[] devices, Color[][] buffers) Setup(params string[] names)
    {
        var devices = names.Select(n => DeviceBuilder.MakeDevice(n, 1)).ToArray();
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

    static void Render(FakeBroker broker, Device[] devices, Color[][] buffers,
        List<RuleBinding> bindings, OpenRgbConfig? config = null)
    {
        var ctx = new RenderContext(broker, devices, buffers, new bool[devices.Length],
            bindings, config ?? new OpenRgbConfig());
        OpenRgbEngine.RenderLayers(in ctx, 1);
    }

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
        var ctx = new RenderContext(broker, devices, buffers, new bool[devices.Length], [], config);

        OpenRgbEngine.RenderStartupFrame(in ctx, 1);

        Assert.Equal(2, broker.UpdateLedsCalls.Count);
    }
}

internal sealed class CaptureEffect(Action<float> capture) : BaseRgbEffect
{
    protected override void ProcessEffect(Device device, string? zoneRegex, string? ledRegex, float value, int frameCount, float transitionSpeed, Color[] buffer)
        => capture(value);
}
