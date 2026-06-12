using FanControl.OpenRGB;
using FanControl.OpenRGB.Rules;
using Xunit;

namespace FanControl.OpenRGB.Tests.Rules;

public class RuleBindingTests
{
    [Fact]
    public void Ctor_ExposesConfigAndControl()
    {
        var config = new RuleConfig { Name = "Test", DeviceRegex = "GPU" };
        var control = new OpenRgbControlSensor("id", "Test");
        var binding = new RuleBinding(config, control);

        Assert.Same(config, binding.Config);
        Assert.Same(control, binding.Control);
    }
}
