using FanControl.OpenRGB.Toolkit;
using OpenRGB.NET;

namespace FanControl.OpenRGB.Tests.Plugin;

internal sealed class FakeBroker : IOpenRgbBroker
{
    public bool Connected { get; set; } = true;
    public List<(int Index, Color[] Colors)> UpdateLedsCalls { get; } = [];

    public Device[] GetAllControllerData() => [];
    public void UpdateLeds(int deviceIndex, Color[] colors) => UpdateLedsCalls.Add((deviceIndex, colors));
    public void Dispose() { }
}
