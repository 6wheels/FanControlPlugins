using System.Reflection;
using FanControl.OpenRGB.Effects;
using FanControl.OpenRGB.Toolkit;
using OpenRGB.NET;
using Xunit;

namespace FanControl.OpenRGB.Tests.Toolkit;

public class EffectDiscoveryTests
{
    private static readonly Assembly PluginAssembly = typeof(BaseRgbEffect).Assembly;

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

    [Fact]
    public void ScansAnyAssembly_FindsTestOnlyEffect()
    {
        // CaptureEffect lives in the test assembly, not the plugin assembly.
        var types = EffectDiscovery.GetConcreteEffectTypes(typeof(EffectDiscoveryTests).Assembly).ToList();
        Assert.Contains(typeof(LocalProbeEffect), types);
    }

    [Fact]
    public void ReflectionTypeLoadException_FiltersToLoadableConcreteEffects()
    {
        // Simulate a partially-loadable assembly: GetTypes() throws but exposes
        // a mix of loadable types (concrete effect, abstract base, unrelated) and
        // a null (the type that failed to load).
        var partial = new ThrowingAssembly([typeof(StaticEffect), typeof(BaseRgbEffect), typeof(string), null]);

        var types = EffectDiscovery.GetConcreteEffectTypes(partial).ToList();

        Assert.Equal([typeof(StaticEffect)], types);
    }

    private sealed class ThrowingAssembly(Type?[] types) : Assembly
    {
        public override Type[] GetTypes() =>
            throw new ReflectionTypeLoadException(types, new Exception?[types.Length]);
    }
}

// Concrete effect declared only in the test assembly, used to prove discovery
// works against an arbitrary assembly.
internal sealed class LocalProbeEffect : BaseRgbEffect
{
    protected override void ProcessEffect(Device device, string? zoneRegex, string? ledRegex, float value, int frameCount, float transitionSpeed, Color[] buffer) { }
}
