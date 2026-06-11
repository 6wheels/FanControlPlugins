using System.Reflection;
using FanControl.OpenRGB.Effects;
using FanControl.OpenRGB.Toolkit;
using Xunit;

namespace FanControl.OpenRGB.Tests.Toolkit;

public class ProgramTests
{
    private static readonly HashSet<string> KnownEffects =
    [
        "AuroraEffect",
        "BlinkEffect",
        "BreathingEffect",
        "GaugeGradientEffect",
        "GradientEffect",
        "ProgressBarEffect",
        "SpatialGradientEffect",
        "StaticEffect",
    ];

    [Fact]
    public void EffectDiscovery_FindsAllKnownConcreteEffects()
    {
        var found = EffectDiscovery.GetConcreteEffectTypes(typeof(BaseRgbEffect).Assembly)
            .Select(t => t.Name)
            .ToHashSet();

        Assert.Equal(KnownEffects, found);
    }

    [Fact]
    public void EffectDiscovery_NoAbstractEffectLeak()
    {
        var concrete = EffectDiscovery.GetConcreteEffectTypes(typeof(BaseRgbEffect).Assembly).ToList();

        Assert.All(concrete, t => Assert.False(t.IsAbstract));
    }
}
