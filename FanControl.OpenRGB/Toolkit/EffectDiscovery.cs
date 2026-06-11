using System.Reflection;
using FanControl.OpenRGB.Effects;

namespace FanControl.OpenRGB.Toolkit;

internal static class EffectDiscovery
{
    public static IEnumerable<Type> GetConcreteEffectTypes(Assembly assembly)
    {
        try
        {
            return assembly.GetTypes()
                .Where(t => t.IsSubclassOf(typeof(BaseRgbEffect)) && !t.IsAbstract);
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types
                .Where(t => t != null && t.IsSubclassOf(typeof(BaseRgbEffect)) && !t.IsAbstract)
                .Cast<Type>();
        }
    }
}
