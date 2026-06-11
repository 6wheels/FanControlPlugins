# Plugin: SystemMetrics
Exposes system performance metrics via `System.Diagnostics.PerformanceCounter`.

## Platform constraint
- **Windows-only API**: throws `PlatformNotSupportedException` off Windows. This is
  fine because all builds and tests run through `dotnet.exe` (Windows runtime), never
  the Linux .NET runtime. Mark Windows-only types with `[SupportedOSPlatform("windows")]`.

## Resource & error handling
- `PerformanceCounter` handles are unmanaged: dispose them via `IDisposable`/`using`.
- Wrap counter reads in `try-catch`; return a fallback on failure.
