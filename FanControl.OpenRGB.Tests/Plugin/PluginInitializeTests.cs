using FanControl.OpenRGB;
using FanControl.OpenRGB.Toolkit;
using Xunit;

namespace FanControl.OpenRGB.Tests.Plugin;

public class PluginInitializeTests
{
    [Fact]
    public void Initialize_MissingConfig_GeneratesTemplate_AndDoesNotStartEngine()
    {
        using var temp = new TempPath();
        var dialog = new FakeDialog();
        var plugin = new OpenRgbPlugin(dialog, new FakeLogger(), new OpenRgbConfig(), configPath: temp.Path);

        plugin.Initialize();

        Assert.False(plugin.EngineStarted); // template generated, nothing to drive
        Assert.True(File.Exists(temp.Path));
        Assert.Single(dialog.Messages);
    }

    [Fact]
    public void Initialize_ValidConfig_StartsEngine()
    {
        using var temp = new TempPath();
        File.WriteAllText(temp.Path, """{ "Framerate": 30 }""");

        // Fake connect + suspended gate so the engine never opens a real socket:
        // the timer ticks are no-ops while suspended.
        var plugin = new OpenRgbPlugin(
            new FakeDialog(),
            new FakeLogger(),
            new OpenRgbConfig(),
            configPath: temp.Path,
            connect: _ => new FakeBroker(),
            suspended: () => true);

        plugin.Initialize();

        Assert.True(plugin.EngineStarted);

        plugin.Close(); // drains the render timer
    }

    [Fact]
    public void Load_AfterInitialize_RegistersSensors_AndPushesToEngine()
    {
        using var temp = new TempPath();
        File.WriteAllText(temp.Path, """{ "Rules": [ { "Name": "GPU", "DeviceRegex": "GPU" } ] }""");
        var plugin = new OpenRgbPlugin(
            new FakeDialog(),
            new FakeLogger(),
            new OpenRgbConfig(),
            configPath: temp.Path,
            connect: _ => new FakeBroker(),
            suspended: () => true);
        plugin.Initialize();
        Assert.True(plugin.EngineStarted);

        var container = new FakeContainer();
        plugin.Load(container); // engine non-null -> exercises _engine.SetBindings

        Assert.Single(container.ControlSensors);
        plugin.Close();
    }

    [Fact]
    public void Close_AfterInitialize_DoesNotThrow()
    {
        using var temp = new TempPath();
        File.WriteAllText(temp.Path, "{}");
        var plugin = new OpenRgbPlugin(
            new FakeDialog(),
            new FakeLogger(),
            new OpenRgbConfig(),
            configPath: temp.Path,
            connect: _ => new FakeBroker(),
            suspended: () => true);

        plugin.Initialize();

        Assert.Null(Record.Exception(() => plugin.Close()));
    }

    private sealed class TempPath : IDisposable
    {
        public string Path { get; } =
            System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"openrgb_init_{Guid.NewGuid():N}.json");

        public void Dispose()
        {
            try { if (File.Exists(Path)) File.Delete(Path); } catch { }
        }
    }
}
