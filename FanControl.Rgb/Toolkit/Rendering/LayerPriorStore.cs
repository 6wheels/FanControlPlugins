using FanControl.Rgb.Rules;
using OpenRGB.NET;

namespace FanControl.Rgb.Toolkit.Rendering
{
  /// <summary>
  /// Per-sink store of each layer's previous-frame output, keyed by rule binding.
  /// Temporal smoothing fades from a layer's own prior (not the shared composite
  /// buffer) so lower layers never bleed in.
  /// <para>
  /// This lives on the <i>renderer</i>, not the <see cref="RuleBinding"/>: the same
  /// binding list is shared by both sinks (OpenRGB frame sink and NZXT command sink),
  /// which run on separate threads with differently shaped buffers. Storing the prior
  /// on the binding would let the two sinks reallocate each other's state every tick —
  /// a data race that both thrashes the smoothing and tears the arrays. One store per
  /// sink keeps the state private to that sink's thread.
  /// </para>
  /// </summary>
  internal sealed class LayerPriorStore
  {
    private readonly Dictionary<RuleBinding, Color[][]> _store = new();

    /// <summary>
    /// Returns a prior-output store shaped like <paramref name="buffers"/>, reusing the
    /// cached one when the shape still matches. Reallocates only when the buffer shape
    /// changed (e.g. an OpenRGB reconnect rebuilt the buffers), which would otherwise
    /// leave a stale store indexing out of bounds.
    /// </summary>
    public Color[][] Ensure(RuleBinding binding, Color[][] buffers)
    {
      if (_store.TryGetValue(binding, out var prior) && ShapeMatches(prior, buffers))
        return prior;

      prior = Array.ConvertAll(buffers, b => new Color[b.Length]);
      _store[binding] = prior;
      return prior;
    }

    /// <summary>Drops all cached state; call when the binding set is replaced.</summary>
    public void Clear() => _store.Clear();

    private static bool ShapeMatches(Color[][] prior, Color[][] buffers)
    {
      if (prior.Length != buffers.Length) return false;
      for (int i = 0; i < buffers.Length; i++)
        if (prior[i].Length != buffers[i].Length) return false;
      return true;
    }
  }
}
