using OpenRGB.NET;

namespace FanControl.Rgb.Toolkit;

// Output side of the NZXT command sink. Pushes per-LED colours to a liquidctl
// channel through the LiquidCtl bridge's named pipe, so they ride the same
// serialized HID queue as the fan commands (no USB contention). Injected so the
// renderer is unit-testable without a real pipe.
internal interface INzxtBridge : IDisposable
{
    bool Connected { get; }
    void SetLeds(string deviceMatch, string channel, Color[] colors);
}
