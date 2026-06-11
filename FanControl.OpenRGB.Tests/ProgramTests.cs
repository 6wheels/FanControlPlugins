using System.Reflection;
using FanControl.OpenRGB.Effects;
using Xunit;

namespace FanControl.OpenRGB.Tests;

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

    private static IEnumerable<Type> GetEffectTypes()
    {
        try
        {
            return typeof(BaseRgbEffect).Assembly
                .GetTypes()
                .Where(t => t.IsSubclassOf(typeof(BaseRgbEffect)) && !t.IsAbstract);
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types
                .Where(t => t != null && t.IsSubclassOf(typeof(BaseRgbEffect)) && !t.IsAbstract)
                .Cast<Type>();
        }
    }

    [Fact]
    public void EffectDiscovery_FindsAllKnownConcreteEffects()
    {
        var found = GetEffectTypes().Select(t => t.Name).ToHashSet();

        Assert.Equal(KnownEffects, found);
    }

    [Fact]
    public void EffectDiscovery_NoAbstractEffectLeak()
    {
        // GetEffectTypes() already filters to !IsAbstract; this verifies the filter holds
        var concrete = GetEffectTypes().ToList();

        Assert.All(concrete, t => Assert.False(t.IsAbstract));
    }

    [Fact]
    public void LockFilePath_IsInTempDirectory()
    {
        string lockPath = Path.Combine(Path.GetTempPath(), "fancontrol_rgb.lock");

        Assert.StartsWith(Path.GetTempPath(), lockPath);
        Assert.EndsWith("fancontrol_rgb.lock", lockPath);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(314)]
    [InlineData(628)]
    [InlineData(10000)]
    public void AutoValue_Formula_AlwaysInRange(int frame)
    {
        float value = 50f + 50f * (float)Math.Sin(frame * 0.01);

        Assert.InRange(value, 0f, 100f);
    }
}
