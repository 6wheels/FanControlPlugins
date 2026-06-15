namespace FanControl.Rgb.Toolkit.Rendering;

// Synthetic renderable device for an NZXT target. Its channels (ring/logo,
// led1/led2) are exposed as 1D zones in the same namespace the rules' regex
// searches, so effects render onto it exactly like an OpenRGB device. The
// per-channel slices map regions of the flat frame buffer back to liquidctl
// channel commands at flush time.
internal sealed class NzxtDevice : IRgbDevice
{
    public string? Name { get; }
    public IReadOnlyList<IRgbZone> Zones { get; }
    public IReadOnlyList<IRgbLed> Leds { get; }

    // Flush map: where each channel lives in the flat buffer.
    public IReadOnlyList<NzxtChannelSlice> Channels { get; }
    public int TotalLeds { get; }

    public NzxtDevice(string name, string deviceMatch, IReadOnlyList<NzxtChannel> channels)
    {
        Name = name;

        var zones = new List<IRgbZone>(channels.Count);
        var leds = new List<IRgbLed>();
        var slices = new List<NzxtChannelSlice>(channels.Count);

        int offset = 0;
        foreach (var channel in channels)
        {
            zones.Add(new SimpleZone(channel.Name, (uint)channel.LedCount));
            for (int i = 0; i < channel.LedCount; i++)
                leds.Add(new SimpleLed($"{channel.Name} {i}"));
            slices.Add(new NzxtChannelSlice(deviceMatch, channel.Name, offset, channel.LedCount));
            offset += channel.LedCount;
        }

        Zones = zones;
        Leds = leds;
        Channels = slices;
        TotalLeds = offset;
    }

    private sealed class SimpleZone(string name, uint ledCount) : IRgbZone
    {
        public string Name { get; } = name;
        public uint LedCount { get; } = ledCount;
        public IRgbMatrix? MatrixMap => null; // NZXT strips/rings are 1D
    }

    private sealed class SimpleLed(string name) : IRgbLed
    {
        public string Name { get; } = name;
    }
}

internal readonly record struct NzxtChannelSlice(string DeviceMatch, string ChannelName, int Offset, int Count);

internal static class NzxtDeviceFactory
{
    public static NzxtDevice[] Build(NzxtConfig config) =>
        config.Targets
            .Where(t => !string.IsNullOrWhiteSpace(t.Name) && t.Channels.Count > 0)
            .Select(t => new NzxtDevice(t.Name, t.DeviceMatch, t.Channels))
            .ToArray();
}
