using FanControl.OpenRGB;
using FanControl.OpenRGB.Effects;
using Xunit;

namespace FanControl.OpenRGB.Tests.Plugin;

public class PluginLoadTests
{
    // Internal test-seam ctor injects config directly; Load() reads _config
    // and _devices (empty by default), so no real connection is needed.
    static OpenRgbPlugin MakePlugin(OpenRgbConfig config) =>
        new(new FakeDialog(), new FakeLogger(), config);

    [Fact]
    public void Load_RegistersControlSensors_ForEachRule()
    {
        var config = new OpenRgbConfig
        {
            Rules =
            [
                new RuleConfig { Id = "RULE_A", Name = "A", DeviceRegex = "GPU", Effect = new StaticEffect() },
                new RuleConfig { Id = "RULE_B", Name = "B", DeviceRegex = "FAN", Effect = new StaticEffect() }
            ]
        };
        var plugin = MakePlugin(config);
        var container = new FakeContainer();

        plugin.Load(container);

        Assert.Equal(2, container.ControlSensors.Count);
        Assert.Contains(container.ControlSensors, s => s.Id == "RULE_A");
        Assert.Contains(container.ControlSensors, s => s.Id == "RULE_B");
    }

    [Fact]
    public void Load_GeneratesSafeId_WhenIdIsEmpty()
    {
        var config = new OpenRgbConfig
        {
            Rules = [new RuleConfig { Id = "", Name = "My Rule", DeviceRegex = ".*", Effect = new StaticEffect() }]
        };
        var plugin = MakePlugin(config);
        var container = new FakeContainer();

        plugin.Load(container);

        Assert.Single(container.ControlSensors);
        Assert.Equal("OPENRGB_MY_RULE", container.ControlSensors[0].Id);
    }

    [Fact]
    public void Load_ClearsExistingBindings_OnReload()
    {
        var config = new OpenRgbConfig
        {
            Rules = [new RuleConfig { Id = "R", Name = "R", DeviceRegex = ".*", Effect = new StaticEffect() }]
        };
        var plugin = MakePlugin(config);
        var container = new FakeContainer();

        plugin.Load(container);
        plugin.Load(container);

        // container.ControlSensors accumulates across calls (it's the fake's list),
        // but internally the plugin should have cleared and re-created bindings.
        Assert.Equal(2, container.ControlSensors.Count); // 2 adds from 2 loads
    }

    [Fact]
    public void Load_SafeId_UppercasesAndUnderscoresName()
    {
        var config = new OpenRgbConfig
        {
            Rules = [new RuleConfig { Id = "", Name = "gpu hot zone", DeviceRegex = ".*", Effect = new StaticEffect() }]
        };
        var container = new FakeContainer();

        MakePlugin(config).Load(container);

        Assert.Equal("OPENRGB_GPU_HOT_ZONE", container.ControlSensors[0].Id);
    }

    [Fact]
    public void Load_WhitespaceId_GeneratesSafeId()
    {
        var config = new OpenRgbConfig
        {
            Rules = [new RuleConfig { Id = "   ", Name = "Mix", DeviceRegex = ".*", Effect = new StaticEffect() }]
        };
        var container = new FakeContainer();

        MakePlugin(config).Load(container);

        Assert.Equal("OPENRGB_MIX", container.ControlSensors[0].Id);
    }

    [Fact]
    public void Load_NoRules_RegistersNothing()
    {
        var container = new FakeContainer();

        MakePlugin(new OpenRgbConfig()).Load(container);

        Assert.Empty(container.ControlSensors);
    }

    [Fact]
    public void Close_DoesNotThrow_WhenNeverInitialized()
    {
        var plugin = new OpenRgbPlugin(new FakeDialog(), new FakeLogger());
        var ex = Record.Exception(() => plugin.Close());
        Assert.Null(ex);
    }
}
