using FanControl.Plugins;
using FanControl.SystemMetrics.Sampling;

namespace FanControl.SystemMetrics;

// Thin IPluginSensor: holds identity and the latest value. All counter wiring and
// per-metric quirks are delegated to an IMetricSampler.
internal sealed class MetricSensor : IPluginSensor, IDisposable
{
    private readonly IMetricSampler _sampler;

    public string Id { get; }
    public string Name { get; }
    public float? Value { get; private set; }

    public MetricSensor(string id, string name, IMetricSampler sampler)
    {
        Id = id;
        Name = name;
        _sampler = sampler;
    }

    public void Update()
    {
        try
        {
            Value = _sampler.Sample();
        }
        catch
        {
            Value = 0f; // secure fallback value
        }
    }

    public void Dispose() => _sampler.Dispose();
}
