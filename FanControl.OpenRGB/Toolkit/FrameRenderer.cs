using FanControl.OpenRGB.Effects;
using OpenRGB.NET;

namespace FanControl.OpenRGB.Toolkit;

internal static class FrameRenderer
{
    public static void RenderAndFlush(
        BaseRgbEffect effect,
        IOpenRgbBroker broker,
        Device[] devices,
        string deviceRegex,
        string? zoneRegex,
        string? ledRegex,
        float value,
        int frame,
        float transitionSpeed,
        Color[][] frameBuffers)
    {
        effect.Apply(devices, deviceRegex, zoneRegex, ledRegex, value, frame, transitionSpeed, frameBuffers);
        for (int i = 0; i < frameBuffers.Length; i++)
            broker.UpdateLeds(i, frameBuffers[i]);
    }

    public static void SetAllColor(IOpenRgbBroker broker, Color color)
    {
        try
        {
            var devices = broker.GetAllControllerData();
            for (int i = 0; i < devices.Length; i++)
            {
                var colors = Enumerable.Repeat(color, devices[i].Leds.Length).ToArray();
                broker.UpdateLeds(i, colors);
            }
        }
        catch { }
    }
}
