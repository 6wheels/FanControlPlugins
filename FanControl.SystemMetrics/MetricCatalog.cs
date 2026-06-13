namespace FanControl.SystemMetrics;

// Single source of truth for the metrics this plugin can expose. Add a new entry
// here (and its key to the config) to extend the plugin — no Load() change needed.
internal static class MetricCatalog
{
    public static readonly IReadOnlyList<MetricDefinition> All =
    [
        new("CpuLoad",  "SYS_CPU_LOAD",  "CPU Total Load (%)",   "Processor",    "% Processor Time",       "_Total"),
        new("GpuLoad",  "SYS_GPU_LOAD",  "GPU Total Load (%)",   "GPU Engine",   "Utilization Percentage", "*engtype_3D", true),
        new("DiskTime", "SYS_DISK_TIME", "Disk Active Time (%)", "PhysicalDisk", "% Disk Time",            "_Total"),
    ];
}
