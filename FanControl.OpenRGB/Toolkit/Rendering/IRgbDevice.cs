namespace FanControl.OpenRGB.Toolkit.Rendering;

// Sink-agnostic view of a renderable device. Effects target these members only,
// so the same effect pipeline drives both the OpenRGB frame sink (real
// OpenRGB.NET devices) and the NZXT command sink (synthetic devices). OpenRGB.NET
// Device has an internal constructor and can't be synthesised, hence this seam.
public interface IRgbDevice
{
    string? Name { get; }
    IReadOnlyList<IRgbZone> Zones { get; }
    IReadOnlyList<IRgbLed> Leds { get; }
}

public interface IRgbZone
{
    string Name { get; }
    uint LedCount { get; }
    IRgbMatrix? MatrixMap { get; }
}

public interface IRgbLed
{
    string Name { get; }
}

public interface IRgbMatrix
{
    uint Width { get; }
    uint Height { get; }
    uint[,] Matrix { get; }
}
