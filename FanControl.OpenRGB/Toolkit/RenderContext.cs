using System.Diagnostics;
using FanControl.OpenRGB.Rules;
using OpenRGB.NET;

namespace FanControl.OpenRGB.Toolkit;

// Stable per-render state bundled to keep RenderFrame's signature small.
// A readonly record struct so it lives on the stack — built fresh each tick
// without heap allocation, satisfying the allocation-free render-loop rule.
// Ordered hardware → frame buffers → functional logic.
internal readonly record struct RenderContext(
    // Hardware
    IOpenRgbBroker Broker,
    Device[] Devices,
    // Frame buffers
    Color[][] Buffers,
    bool[] DeviceNeedsUpdate,
    // Functional
    List<RuleBinding> Bindings,
    OpenRgbConfig Config,
    Stopwatch StartupStopwatch);
