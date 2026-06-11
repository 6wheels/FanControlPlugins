# FanControl.SystemMetrics

A FanControl plugin that exposes Windows **performance counters** as sensors, letting you build fan curves driven by system activity (CPU, GPU, disk) instead of only hardware temperatures.

## Sensors

| Sensor | Source counter | Description |
| --- | --- | --- |
| **CPU Total Load (%)** | `Processor \ % Processor Time \ _Total` | Overall CPU utilization. |
| **GPU Total Load (%)** | `GPU Engine \ Utilization Percentage \ *engtype_3D` | Sum of all 3D GPU engine instances, clamped to 0–100%. |
| **Disk Active Time (%)** | `PhysicalDisk \ % Disk Time \ _Total` | How busy the physical disks are. |

> **Note:** The sensors are registered in FanControl's temperature pool so they appear in the UI. Their values are percentages (0–100), not degrees.

## ⚙️ Prerequisites

* Windows performance counters must be available (they are part of Windows by default). If a counter is missing, that sensor reports `0` instead of crashing the plugin.

## 📝 Configuration

None. The plugin auto-registers its sensors on load — no config file is required.

## 📚 Dependencies

This project uses:
- **[System.Diagnostics.PerformanceCounter](https://github.com/dotnet/runtime) 10.0.8** — MIT license — embedded into the output `.dll` via Costura.Fody (bundling permitted by MIT).
- **[FanControl.Plugins](https://github.com/Rem0o/FanControl.Releases)** — the host plugin API (proprietary; supplied by FanControl at runtime, **not** redistributed).
