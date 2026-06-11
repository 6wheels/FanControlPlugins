using FanControl.OpenRGB.Effects;
using FanControl.OpenRGB.Toolkit;
using OpenRGB.NET;
using FanControl.OpenRGB.Tests;
using Xunit;

namespace FanControl.OpenRGB.Tests.Toolkit;

public class FrameRendererTests
{
    private sealed class FakeBroker : IOpenRgbBroker
    {
        public bool Connected => true;
        public List<(int DeviceIndex, Color[] Colors)> UpdateCalls { get; } = [];
        private readonly Device[] _devices;

        public FakeBroker(Device[] devices) => _devices = devices;

        public Device[] GetAllControllerData() => _devices;
        public void UpdateLeds(int deviceIndex, Color[] colors) => UpdateCalls.Add((deviceIndex, colors));
        public void Dispose() { }
    }

    [Fact]
    public void RenderAndFlush_CallsUpdateLedsOncePerDevice()
    {
        var d1 = DeviceBuilder.MakeDevice("GPU", 3);
        var d2 = DeviceBuilder.MakeDevice("Fan", 2);
        var broker = new FakeBroker([d1, d2]);
        var effect = new StaticEffect { ColorHex = "#FF0000", ModulateByValue = false };
        var buffers = new Color[][] { new Color[3], new Color[2] };

        FrameRenderer.RenderAndFlush(effect, broker, [d1, d2], ".*", null, null, 100f, 0, 1f, buffers);

        Assert.Equal(2, broker.UpdateCalls.Count);
        Assert.Equal(0, broker.UpdateCalls[0].DeviceIndex);
        Assert.Equal(1, broker.UpdateCalls[1].DeviceIndex);
    }

    [Fact]
    public void RenderAndFlush_PassesRenderedColorsToClient()
    {
        var device = DeviceBuilder.MakeDevice("GPU", 2);
        var broker = new FakeBroker([device]);
        var effect = new StaticEffect { ColorHex = "#FF0000", ModulateByValue = false };
        var buffers = new Color[][] { new Color[2] };

        FrameRenderer.RenderAndFlush(effect, broker, [device], ".*", null, null, 100f, 0, 1f, buffers);

        var flushed = broker.UpdateCalls[0].Colors;
        Assert.All(flushed, c =>
        {
            Assert.Equal(0xFF, c.R);
            Assert.Equal(0x00, c.G);
            Assert.Equal(0x00, c.B);
        });
    }

    [Fact]
    public void SetAllColor_SetsEveryDeviceLedToColor()
    {
        var d1 = DeviceBuilder.MakeDevice("GPU", 3);
        var d2 = DeviceBuilder.MakeDevice("Fan", 2);
        var broker = new FakeBroker([d1, d2]);
        var blue = new Color(0, 0, 255);

        FrameRenderer.SetAllColor(broker, blue);

        Assert.Equal(2, broker.UpdateCalls.Count);
        Assert.All(broker.UpdateCalls[0].Colors, c => Assert.Equal(255, c.B));
        Assert.All(broker.UpdateCalls[1].Colors, c => Assert.Equal(255, c.B));
    }

    [Fact]
    public void SetAllColor_DoesNotThrow_WhenBrokerThrows()
    {
        var broker = new ThrowingBroker();

        var ex = Record.Exception(() => FrameRenderer.SetAllColor(broker, new Color(0, 0, 0)));

        Assert.Null(ex);
    }

    private sealed class ThrowingBroker : IOpenRgbBroker
    {
        public bool Connected => true;
        public Device[] GetAllControllerData() => throw new InvalidOperationException("simulated failure");
        public void UpdateLeds(int deviceIndex, Color[] colors) { }
        public void Dispose() { }
    }
}
