using FanControl.Plugins;

namespace FanControl.SystemMetrics
{
  public class MetricsPlugin : IPlugin
  {
    // Expression-bodied property
    public string Name => "System Metrics";

    public void Initialize() { }

    public void Load(IPluginSensorsContainer container)
    {
      // Utilisation du target-typed new
      MetricSensor cpuLoad = new(
          "SYS_CPU_LOAD",
          "CPU Total Load (%)",
          "Processor", "% Processor Time", "_Total"
      );

      MetricSensor gpuLoad = new(
          "SYS_GPU_LOAD",
          "GPU Total Load (%)",
          "GPU Engine", "Utilization Percentage", "*engtype_3D",
          isGpuCounter: true
      );

      MetricSensor diskActivity = new(
          "SYS_DISK_TIME",
          "Disk Active Time (%)",
          "PhysicalDisk", "% Disk Time", "_Total"
      );

      // Injection dans le pool des températures pour contourner le filtre UI
      container.TempSensors.Add(cpuLoad);
      container.TempSensors.Add(gpuLoad);
      container.TempSensors.Add(diskActivity);
    }

    public void Close() { }
  }
}