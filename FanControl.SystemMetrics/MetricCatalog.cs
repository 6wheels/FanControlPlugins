namespace FanControl.SystemMetrics;

// Single source of truth for the metrics this plugin can expose. Add a new entry
// here (and its key to the config) to extend the plugin — no Load() change needed.
internal static class MetricCatalog
{
    private const float BytesToMb = 1f / (1024f * 1024f);

    public static readonly IReadOnlyList<MetricDefinition> All =
    [
        new("CpuLoad",   "SYS_CPU_LOAD",   "CPU Total Load (%)",   "Processor",             "% Processor Time",        "_Total"),
        new("GpuLoad",   "SYS_GPU_LOAD",   "GPU Total Load (%)",   "GPU Engine",            "Utilization Percentage",  "*engtype_3D", SampleMode.Aggregate),
        new("DiskTime",  "SYS_DISK_TIME",  "Disk Active Time (%)", "PhysicalDisk",          "% Disk Time",             "_Total"),
        new("NetworkIO", "SYS_NET_IO",     "Network I/O (%)",      "Network Interface",     "Bytes Total/sec",         "", SampleMode.Network),
        new("DiskRead",  "SYS_DISK_READ",  "Disk Read (MB/s)",     "PhysicalDisk",          "Disk Read Bytes/sec",     "_Total", SampleMode.Single, BytesToMb),
        new("DiskWrite", "SYS_DISK_WRITE", "Disk Write (MB/s)",    "PhysicalDisk",          "Disk Write Bytes/sec",    "_Total", SampleMode.Single, BytesToMb),
        new("RamUsage",  "SYS_RAM_USAGE",  "RAM Usage (%)",        "Memory",                "% Committed Bytes In Use", ""),
        new("CpuFreq",   "SYS_CPU_FREQ",   "CPU Frequency (%)",    "Processor Information", "% Processor Performance", "_Total"),
        new("PageFile",  "SYS_PAGEFILE",   "Page File Usage (%)",  "Paging File",           "% Usage",                 "_Total"),
    ];
}
