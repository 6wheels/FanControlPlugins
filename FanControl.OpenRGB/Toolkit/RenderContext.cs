using FanControl.OpenRGB.Rules;
using OpenRGB.NET;

namespace FanControl.OpenRGB.Toolkit;

// Stable per-render state bundled to keep the render helpers' signatures small.
// A readonly record struct so it lives on the stack — built fresh each tick
// without heap allocation, satisfying the allocation-free render-loop rule.
// Ordered hardware → frame buffers → functional logic. Timing lives in the
// engine (injected TimeProvider), not here.
internal readonly record struct RenderContext(
    // Hardware
    IOpenRgbBroker Broker,
    Device[] Devices,
    // Frame buffers
    Color[][] Buffers,
    bool[] DeviceNeedsUpdate,
    // Functional
    IReadOnlyList<RuleBinding> Bindings,
    OpenRgbConfig Config);
