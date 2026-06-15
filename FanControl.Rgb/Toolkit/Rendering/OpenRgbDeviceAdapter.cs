using OpenRGB.NET;

namespace FanControl.Rgb.Toolkit.Rendering;

// Wraps an OpenRGB.NET Device behind IRgbDevice. Built once per connect (not per
// frame) so the render loop stays allocation-free. Index alignment with the
// broker's Device[] is preserved by wrapping in the same order.
internal sealed class OpenRgbDeviceAdapter : IRgbDevice
{
    public OpenRgbDeviceAdapter(Device device)
    {
        Name = device.Name;
        Zones = Array.ConvertAll(device.Zones, z => (IRgbZone)new ZoneAdapter(z));
        Leds = Array.ConvertAll(device.Leds, l => (IRgbLed)new LedAdapter(l));
    }

    public string? Name { get; }
    public IReadOnlyList<IRgbZone> Zones { get; }
    public IReadOnlyList<IRgbLed> Leds { get; }

    private sealed class ZoneAdapter : IRgbZone
    {
        public ZoneAdapter(Zone zone)
        {
            Name = zone.Name;
            LedCount = zone.LedCount;
            MatrixMap = zone.MatrixMap is { } m ? new MatrixAdapter(m) : null;
        }

        public string Name { get; }
        public uint LedCount { get; }
        public IRgbMatrix? MatrixMap { get; }
    }

    private sealed class LedAdapter : IRgbLed
    {
        public LedAdapter(Led led) => Name = led.Name;
        public string Name { get; }
    }

    private sealed class MatrixAdapter : IRgbMatrix
    {
        public MatrixAdapter(MatrixMap matrix)
        {
            Width = matrix.Width;
            Height = matrix.Height;
            Matrix = matrix.Matrix;
        }

        public uint Width { get; }
        public uint Height { get; }
        public uint[,] Matrix { get; }
    }
}
