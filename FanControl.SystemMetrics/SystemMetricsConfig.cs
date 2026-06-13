namespace FanControl.SystemMetrics;

// User-editable config. Only metrics whose catalog key appears in EnabledMetrics
// are registered. Defaults to every known key so the generated template exposes all.
internal sealed class SystemMetricsConfig
{
    public List<string> EnabledMetrics { get; set; } =
        MetricCatalog.All.Select(d => d.Key).ToList();

    // Link speed (Mbps) used to normalize the NetworkIO metric to 0-100%.
    public int NetworkLinkSpeedMbps { get; set; } = 1000;
}
