using FanControl.Rgb;
using FanControl.Rgb.Rules;
using Xunit;

namespace FanControl.Rgb.Tests.Rules;

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
