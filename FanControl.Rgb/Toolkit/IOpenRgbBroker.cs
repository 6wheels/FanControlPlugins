using OpenRGB.NET;

namespace FanControl.Rgb.Toolkit;

internal interface IOpenRgbBroker : IDisposable
{
    bool Connected { get; }
    Device[] GetAllControllerData();
    void UpdateLeds(int deviceIndex, Color[] colors);
}
