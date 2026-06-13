namespace FanControl.SystemMetrics;

// Descriptor for a single available metric: the config key the user toggles plus
// the PerformanceCounter wiring needed to build the sensor. Keeps the sensor
// catalog declarative and out of the plugin's Load().
internal sealed record MetricDefinition(
    string Key,
    string Id,
    string Name,
    string Category,
    string Counter,
    string Instance,
    bool IsGpu = false)
{
    public MetricSensor CreateSensor() => new(Id, Name, Category, Counter, Instance, IsGpu);
}
