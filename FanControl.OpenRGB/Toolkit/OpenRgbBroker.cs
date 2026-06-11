using OpenRGB.NET;

namespace FanControl.OpenRGB.Toolkit;

internal sealed class OpenRgbBroker : IOpenRgbBroker
{
    private readonly OpenRgbClient _client;

    public OpenRgbBroker(OpenRgbClient client) => _client = client;

    public bool Connected => _client.Connected;
    public Device[] GetAllControllerData() => _client.GetAllControllerData();
    public void UpdateLeds(int deviceIndex, Color[] colors) => _client.UpdateLeds(deviceIndex, colors);
    public void Dispose() => _client.Dispose();
}
