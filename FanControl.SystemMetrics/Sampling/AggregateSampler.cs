using System.Diagnostics;
using System.Runtime.Versioning;

namespace FanControl.SystemMetrics.Sampling;

// Sums a counter across every instance whose name matches a suffix, clamped to 0-100.
// Used for metrics split across many instances with no _Total (e.g. GPU Engine).
[SupportedOSPlatform("windows")]
internal sealed class AggregateSampler : IMetricSampler
{
    private readonly PerformanceCounter[] _counters = [];

    public AggregateSampler(string category, string counter, string instanceSuffix)
    {
        try
        {
            PerformanceCounterCategory cat = new(category);
            var instances = cat.GetInstanceNames()
                               .Where(x => x.EndsWith(instanceSuffix, StringComparison.OrdinalIgnoreCase));

            _counters = instances.Select(inst => new PerformanceCounter(category, counter, inst)).ToArray();
            foreach (PerformanceCounter c in _counters)
            {
                c.NextValue(); // prime
            }
        }
        catch
        {
            // Missing category/instances must not crash startup.
        }
    }

    public float Sample()
    {
        float total = 0f;
        foreach (PerformanceCounter c in _counters)
        {
            try { total += c.NextValue(); } catch { }
        }
        return Math.Clamp(total, 0f, 100f);
    }

    public void Dispose()
    {
        foreach (PerformanceCounter c in _counters)
        {
            c.Dispose();
        }
    }
}
