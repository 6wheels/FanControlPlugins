using FanControl.OpenRGB.Toolkit;
using OpenRGB.NET;

namespace FanControl.OpenRGB.Tests.Plugin;

internal sealed class FakeBroker : IOpenRgbBroker
{
    public bool Connected { get; set; } = true;
    public Device[] Devices { get; set; } = [];
    public List<(int Index, Color[] Colors)> UpdateLedsCalls { get; } = [];
    public bool Disposed { get; private set; }

    // Simulate transient USB hiccups while still connected: throw this many
    // times before succeeding again.
    public int FailUpdatesWhileConnected { get; set; }

    public Device[] GetAllControllerData() => Devices;

    public void UpdateLeds(int deviceIndex, Color[] colors)
    {
        if (!Connected) throw new InvalidOperationException("not connected");
        if (FailUpdatesWhileConnected > 0)
        {
            FailUpdatesWhileConnected--;
            throw new InvalidOperationException("usb hiccup");
        }
        UpdateLedsCalls.Add((deviceIndex, colors));
    }

    public void Dispose() => Disposed = true;
}
