namespace FanControl.SystemMetrics.Sampling;

// One sampling strategy per metric kind. Isolates the PerformanceCounter wiring and
// any unit/normalization quirks from the sensor, keeping MetricCatalog declarative.
internal interface IMetricSampler : IDisposable
{
    // Returns the current metric value. May throw; the caller guards and falls back.
    float Sample();
}
