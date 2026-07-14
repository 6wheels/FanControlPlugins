using FanControl.Rgb;
using FanControl.Rgb.Rules;
using FanControl.Rgb.Toolkit.Rendering;
using OpenRGB.NET;
using Xunit;

namespace FanControl.Rgb.Tests.Toolkit;

public class LayerPriorStoreTests
{
    private static RuleBinding NewBinding()
        => new(new RuleConfig { DeviceRegex = "GPU" }, new OpenRgbControlSensor("id", "Test"));

    [Fact]
    public void Ensure_MatchesBufferShape()
    {
        var store = new LayerPriorStore();
        var buffers = new[] { new Color[3], new Color[5] };

        var prior = store.Ensure(NewBinding(), buffers);

        Assert.Equal(2, prior.Length);
        Assert.Equal(3, prior[0].Length);
        Assert.Equal(5, prior[1].Length);
    }

    [Fact]
    public void Ensure_ReusesStoreAcrossTicksWhenShapeUnchanged()
    {
        var store = new LayerPriorStore();
        var binding = NewBinding();
        var buffers = new[] { new Color[4] };

        var first = store.Ensure(binding, buffers);
        var second = store.Ensure(binding, buffers);

        Assert.Same(first, second);
    }

    // A reconnect rebuilds the sink buffers with a new shape; the stale store must be
    // reallocated instead of indexing out of bounds (issue #24 regression).
    [Fact]
    public void Ensure_ReallocatesWhenBufferShapeChanges()
    {
        var store = new LayerPriorStore();
        var binding = NewBinding();

        store.Ensure(binding, new[] { new Color[8] });
        var afterReconnect = store.Ensure(binding, new[] { new Color[8], new Color[16] });

        Assert.Equal(2, afterReconnect.Length);
        Assert.Equal(16, afterReconnect[1].Length);
    }

    // Same device count but a device's LED count changed: the per-element shape check must
    // also trigger a realloc, not just the top-level device-count change.
    [Fact]
    public void Ensure_ReallocatesWhenPerDeviceLedCountChanges()
    {
        var store = new LayerPriorStore();
        var binding = NewBinding();

        var before = store.Ensure(binding, new[] { new Color[8], new Color[4] });
        var after = store.Ensure(binding, new[] { new Color[8], new Color[6] });

        Assert.NotSame(before, after);
        Assert.Equal(6, after[1].Length);
    }

    // The same binding is shared by both sinks, which have differently shaped buffers.
    // Each sink owns its own store, so they must never share (or thrash) prior state —
    // the root cause of the flicker + crash regression.
    [Fact]
    public void SeparateStores_KeepPerSinkStateIndependent()
    {
        var binding = NewBinding();
        var openRgb = new LayerPriorStore();
        var nzxt = new LayerPriorStore();

        var openRgbPrior = openRgb.Ensure(binding, new[] { new Color[10] });   // frame sink shape
        var nzxtPrior = nzxt.Ensure(binding, new[] { new Color[8], new Color[1] }); // command sink shape

        Assert.NotSame(openRgbPrior, nzxtPrior);
        Assert.Single(openRgbPrior);
        Assert.Equal(2, nzxtPrior.Length);
        // Re-querying each store returns its own stable state, unaffected by the other sink.
        Assert.Same(openRgbPrior, openRgb.Ensure(binding, new[] { new Color[10] }));
        Assert.Same(nzxtPrior, nzxt.Ensure(binding, new[] { new Color[8], new Color[1] }));
    }

    [Fact]
    public void Clear_DropsCachedState()
    {
        var store = new LayerPriorStore();
        var binding = NewBinding();
        var buffers = new[] { new Color[4] };

        var first = store.Ensure(binding, buffers);
        store.Clear();
        var afterClear = store.Ensure(binding, buffers);

        Assert.NotSame(first, afterClear);
    }
}
