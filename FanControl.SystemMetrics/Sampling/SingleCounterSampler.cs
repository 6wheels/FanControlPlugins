using System.Diagnostics;
using System.Runtime.Versioning;

namespace FanControl.SystemMetrics.Sampling;

// Reads one PerformanceCounter and applies an optional unit scale (e.g. bytes -> MB).
[SupportedOSPlatform("windows")]
internal sealed class SingleCounterSampler : IMetricSampler
{
    private readonly PerformanceCounter? _counter;
    private readonly float _scale;

    public SingleCounterSampler(string category, string counter, string instance, float scale = 1f)
    {
        _scale = scale;
        try
        {
            // Single-instance categories (e.g. Memory) take the 2-arg ctor.
            _counter = string.IsNullOrEmpty(instance)
                ? new PerformanceCounter(category, counter)
                : new PerformanceCounter(category, counter, instance);
            _counter.NextValue(); // prime; first read is always 0
        }
        catch
        {
            // A missing counter must not crash the plugin at startup.
            _counter = null;
        }
    }

    public float Sample() => _counter is null ? 0f : _counter.NextValue() * _scale;

    public void Dispose() => _counter?.Dispose();
}
