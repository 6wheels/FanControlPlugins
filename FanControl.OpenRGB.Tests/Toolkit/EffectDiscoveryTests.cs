using FanControl.OpenRGB.Effects;
using FanControl.OpenRGB.Toolkit;
using Xunit;

namespace FanControl.OpenRGB.Tests.Toolkit;

public class EffectDiscoveryTests
{
    private static readonly System.Reflection.Assembly PluginAssembly = typeof(BaseRgbEffect).Assembly;

    [Fact]
    public void ReturnsKnownConcreteEffects()
    {
        var types = EffectDiscovery.GetConcreteEffectTypes(PluginAssembly).ToList();

        Assert.Contains(typeof(StaticEffect), types);
        Assert.Contains(typeof(BlinkEffect), types);
        Assert.Contains(typeof(GradientEffect), types);
        Assert.Contains(typeof(BreathingEffect), types);
        Assert.Contains(typeof(GaugeGradientEffect), types);
    }

    [Fact]
    public void DoesNotReturnAbstractBase()
    {
        var types = EffectDiscovery.GetConcreteEffectTypes(PluginAssembly).ToList();
        Assert.DoesNotContain(typeof(BaseRgbEffect), types);
    }

    [Fact]
    public void AllReturnedTypesAreConcreteSubclasses()
    {
        var types = EffectDiscovery.GetConcreteEffectTypes(PluginAssembly).ToList();
        Assert.All(types, t => Assert.True(t.IsSubclassOf(typeof(BaseRgbEffect)) && !t.IsAbstract));
    }
}
