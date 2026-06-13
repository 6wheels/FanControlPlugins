using FanControl.SystemMetrics.Sampling;

namespace FanControl.SystemMetrics;

// How a metric's raw counter(s) turn into a value.
internal enum SampleMode
{
    Single,    // one counter, optional unit scale
    Aggregate, // sum of matching instances, clamped 0-100 (e.g. GPU)
    Network    // sum of NIC throughput normalized to % of link speed
}

// Descriptor for a single available metric: the config key the user toggles plus the
// PerformanceCounter wiring needed to build the sensor. Keeps the sensor catalog
// declarative and out of the plugin's Load().
internal sealed record MetricDefinition(
    string Key,
    string Id,
    string Name,
    string Category,
    string Counter,
    string Instance,
    SampleMode Mode = SampleMode.Single,
    float Scale = 1f)
{
    public MetricSensor CreateSensor(SystemMetricsConfig config)
    {
        IMetricSampler sampler = Mode switch
        {
            SampleMode.Aggregate => new AggregateSampler(Category, Counter, Instance),
            SampleMode.Network => new NetworkSampler(config.NetworkLinkSpeedMbps),
            _ => new SingleCounterSampler(Category, Counter, Instance, Scale),
        };
        return new MetricSensor(Id, Name, sampler);
    }
}
