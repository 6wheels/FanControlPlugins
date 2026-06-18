using FanControl.Rgb;
using FanControl.Rgb.Effects;
using FanControl.Rgb.Rules;
using FanControl.Rgb.Toolkit;
using FanControl.Rgb.Toolkit.Rendering;
using OpenRGB.NET;
using Xunit;

namespace FanControl.Rgb.Tests.Plugin;

public class NzxtRendererTests
{
    private static NzxtConfig KrakenConfig() => new()
    {
        Enabled = true,
        Targets =
        [
            new NzxtTarget
            {
                Name = "Kraken X63",
                Channels = [new NzxtChannel { Name = "ring", LedCount = 8 }, new NzxtChannel { Name = "logo", LedCount = 1 }]
            }
        ]
    };

    private static RuleBinding Binding(string deviceRegex, float threshold, float value, BaseRgbEffect? effect = null)
    {
        var config = new RuleConfig
        {
            DeviceRegex = deviceRegex,
            ActivationThreshold = threshold,
            TransitionSpeed = 1f, // snap, so rendered colours are deterministic
            Effect = effect ?? new StaticEffect { ColorHex = "#FFFFFF", ModulateByValue = false }
        };
        var control = new OpenRgbControlSensor("id", "test");
        control.Set(value);
        return new RuleBinding(config, control);
    }

    private static NzxtRenderer Renderer(FakeNzxtBridge bridge, bool suspended = false)
        => new(KrakenConfig(), defaultTransitionSpeed: 1f, bridge, (_, _) => { }, () => suspended);

    [Fact]
    public void Factory_BuildsChannelGeometry()
    {
        var devices = NzxtDeviceFactory.Build(KrakenConfig());

        var device = Assert.Single(devices);
        Assert.Equal("Kraken X63", device.Name);
        Assert.Equal(9, device.TotalLeds);
        Assert.Equal(2, device.Zones.Count);
        Assert.Equal(9, device.Leds.Count);
        Assert.Collection(device.Channels,
            ch => { Assert.Equal("ring", ch.ChannelName); Assert.Equal(0, ch.Offset); Assert.Equal(8, ch.Count); },
            ch => { Assert.Equal("logo", ch.ChannelName); Assert.Equal(8, ch.Offset); Assert.Equal(1, ch.Count); });
    }

    [Fact]
    public void Factory_DropsEmptyTargets()
    {
        var config = new NzxtConfig { Targets = [new NzxtTarget { Name = "", Channels = [] }] };
        Assert.Empty(NzxtDeviceFactory.Build(config));
    }

    [Fact]
    public void BelowThreshold_NoPush()
    {
        var bridge = new FakeNzxtBridge();
        var renderer = Renderer(bridge);
        renderer.SetBindings([Binding("Kraken", threshold: 50f, value: 49f)]);

        renderer.Tick();

        Assert.Empty(bridge.Calls);
    }

    [Fact]
    public void AtThreshold_PushesEveryChannelOnce()
    {
        var bridge = new FakeNzxtBridge();
        var renderer = Renderer(bridge);
        renderer.SetBindings([Binding("Kraken", threshold: 0f, value: 100f)]);

        renderer.Tick();

        Assert.Equal(2, bridge.Calls.Count); // ring + logo
        Assert.Contains(bridge.Calls, c => c.Channel == "ring" && c.Colors.Length == 8);
        Assert.Contains(bridge.Calls, c => c.Channel == "logo" && c.Colors.Length == 1);
        Assert.All(bridge.Calls, c => Assert.All(c.Colors, led =>
        {
            Assert.Equal(255, led.R);
            Assert.Equal(255, led.G);
            Assert.Equal(255, led.B);
        }));
        Assert.All(bridge.Calls, c => Assert.Equal("Kraken X63", c.Device));
    }

    [Fact]
    public void UnchangedFrame_NotRepushed()
    {
        var bridge = new FakeNzxtBridge();
        var renderer = Renderer(bridge);
        renderer.SetBindings([Binding("Kraken", threshold: 0f, value: 100f)]);

        renderer.Tick();
        renderer.Tick();
        renderer.Tick();

        Assert.Equal(2, bridge.Calls.Count); // only the first flush
    }

    [Fact]
    public void ChangedValue_Repushes()
    {
        var bridge = new FakeNzxtBridge();
        var renderer = Renderer(bridge);
        var binding = Binding("Kraken", threshold: 0f, value: 0f,
            new GradientEffect { ColorMinHex = "#000000", ColorMaxHex = "#FF0000" });
        renderer.SetBindings([binding]);

        renderer.Tick();                 // value 0 → black (primed push of both channels)
        int afterFirst = bridge.Calls.Count;
        binding.Control.Set(100f);       // value 100 → red
        renderer.Tick();

        Assert.True(bridge.Calls.Count > afterFirst);
        var last = bridge.Calls[^1];
        Assert.Equal(255, last.Colors[0].R);
        Assert.Equal(0, last.Colors[0].G);
    }

    [Fact]
    public void Suspended_NoPush()
    {
        var bridge = new FakeNzxtBridge();
        var renderer = Renderer(bridge, suspended: true);
        renderer.SetBindings([Binding("Kraken", threshold: 0f, value: 100f)]);

        renderer.Tick();

        Assert.Empty(bridge.Calls);
    }

    [Fact]
    public void NonMatchingRegex_NoPush()
    {
        var bridge = new FakeNzxtBridge();
        var renderer = Renderer(bridge);
        renderer.SetBindings([Binding("Smart Device", threshold: 0f, value: 100f)]);

        renderer.Tick();

        Assert.Empty(bridge.Calls);
    }

    [Fact]
    public void EncodeColors_SerializesAsNumericArrays_NotBase64()
    {
        int[][] encoded = NzxtBridge.EncodeColors([new Color(19, 6, 0), new Color(0, 255, 0)]);
        string json = System.Text.Json.JsonSerializer.Serialize(encoded);
        // byte[] would serialize as base64 strings ("EwYA"); must stay [[r,g,b],...].
        Assert.Equal("[[19,6,0],[0,255,0]]", json);
    }

    [Fact]
    public void FailedSend_RetriesUntilAccepted()
    {
        var bridge = new FakeNzxtBridge { Accept = false };
        var renderer = Renderer(bridge);
        renderer.SetBindings([Binding("Kraken", threshold: 0f, value: 100f)]);

        // Bridge down: every tick force-retries every channel (not primed).
        renderer.Tick();
        renderer.Tick();
        Assert.Equal(4, bridge.Calls.Count); // 2 channels x 2 ticks, all retried

        // Bridge comes up: the pending frame goes through, then primes.
        bridge.Accept = true;
        bridge.Calls.Clear();
        renderer.Tick();
        Assert.Equal(2, bridge.Calls.Count); // ring + logo delivered once
        renderer.Tick();
        Assert.Equal(2, bridge.Calls.Count); // now primed + unchanged -> no resend
    }
}

internal sealed class FakeNzxtBridge : INzxtBridge
{
    // When false, SetLeds simulates a bridge that isn't up yet (returns false).
    public bool Accept { get; set; } = true;
    public bool Connected => Accept;
    public List<(string Device, string Channel, Color[] Colors)> Calls { get; } = [];

    public bool SetLeds(string deviceMatch, string channel, Color[] colors)
    {
        Calls.Add((deviceMatch, channel, (Color[])colors.Clone()));
        return Accept;
    }

    public void Dispose() { }
}
