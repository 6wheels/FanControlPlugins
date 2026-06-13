using System.Diagnostics;
using System.Runtime.Versioning;

namespace FanControl.SystemMetrics.Sampling;

// Sums Bytes Total/sec across all network adapters and normalizes the throughput to a
// percentage of a configurable link speed (Mbps), clamped to 0-100.
[SupportedOSPlatform("windows")]
internal sealed class NetworkSampler : IMetricSampler
{
    private const string Category = "Network Interface";
    private const string Counter = "Bytes Total/sec";

    private readonly PerformanceCounter[] _counters = [];
    private readonly int _linkMbps;

    public NetworkSampler(int linkSpeedMbps)
    {
        _linkMbps = linkSpeedMbps;
        try
        {
            PerformanceCounterCategory cat = new(Category);
            _counters = cat.GetInstanceNames()
                          .Select(inst => new PerformanceCounter(Category, Counter, inst))
                          .ToArray();
            foreach (PerformanceCounter c in _counters)
            {
                c.NextValue(); // prime
            }
        }
        catch
        {
            // Missing adapters/counters must not crash startup.
        }
    }

    public float Sample()
    {
        float bytesPerSec = 0f;
        foreach (PerformanceCounter c in _counters)
        {
            try { bytesPerSec += c.NextValue(); } catch { }
        }
        return ToPercent(bytesPerSec, _linkMbps);
    }

    // Pure throughput -> percent conversion. Bytes/sec -> bits/sec over link capacity.
    public static float ToPercent(float bytesPerSec, int linkMbps)
    {
        if (linkMbps <= 0) return 0f;
        float percent = bytesPerSec * 8f / (linkMbps * 1_000_000f) * 100f;
        return Math.Clamp(percent, 0f, 100f);
    }

    public void Dispose()
    {
        foreach (PerformanceCounter c in _counters)
        {
            c.Dispose();
        }
    }
}
