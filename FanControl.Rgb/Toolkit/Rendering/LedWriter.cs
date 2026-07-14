using OpenRGB.NET;

namespace FanControl.Rgb.Toolkit.Rendering
{
  /// <summary>
  /// Centralized per-layer write into a device's frame buffer. Every effect funnels
  /// its LED writes through here instead of touching the buffer directly, so
  /// compositing, opacity and temporal smoothing live in one place (issue #24).
  /// <para>
  /// A <see cref="Write"/> does three things, in order:
  /// <list type="number">
  ///   <item>temporal smoothing — fade from this layer's <i>own</i> prior output toward
  ///     the target, so lower layers never leak in through the shared buffer;</item>
  ///   <item>black-as-transparent — if enabled and the smoothed color is pure black,
  ///     skip the composite so the layer below shows through;</item>
  ///   <item>opacity composite — blend the smoothed color over the buffer below by
  ///     <c>Opacity</c> (1.0 = full overwrite, the default).</item>
  /// </list>
  /// </para>
  /// When no dedicated prior buffer is supplied the composite buffer doubles as the
  /// prior (single-layer / legacy behavior): temporal smoothing then reads the buffer
  /// itself, exactly as before. Effects that want transparency simply skip the write,
  /// mirroring ProgressBar's empty-transparent model.
  /// </summary>
  public readonly struct LedWriter
  {
    private readonly Color[] _buffer;  // composite frame = layers below
    private readonly Color[] _prior;   // this layer's own prior output; == _buffer when none supplied
    private readonly float _opacity;
    private readonly bool _blackIsTransparent;

    public LedWriter(Color[] buffer, Color[]? prior = null, float opacity = 1f, bool blackIsTransparent = false)
    {
      _buffer = buffer;
      _prior = prior ?? buffer;
      _opacity = Math.Clamp(opacity, 0f, 1f);
      _blackIsTransparent = blackIsTransparent;
    }

    /// <summary>Number of LEDs in the target buffer; used by effects for bounds checks.</summary>
    public int Length => _buffer.Length;

    /// <summary>
    /// Composites <paramref name="target"/> into <paramref name="led"/>: temporal-smooth
    /// against this layer's prior output by <paramref name="fade"/> (>=1 snaps, &lt;=0 is a
    /// no-op), then blend over the layer below by the layer opacity.
    /// </summary>
    public void Write(int led, Color target, float fade)
    {
      Color smoothed = Lerp(_prior[led], target, fade);
      _prior[led] = smoothed;

      // Black-as-transparent: leave the layer below untouched.
      if (_blackIsTransparent && smoothed is { R: 0, G: 0, B: 0 }) return;

      _buffer[led] = Lerp(_buffer[led], smoothed, _opacity);
    }

    /// <summary>Canonical color lerp shared across the effect pipeline. t clamped to [0,1].</summary>
    public static Color Lerp(Color a, Color b, float t)
    {
      t = Math.Clamp(t, 0f, 1f);
      return new Color(
          (byte)(a.R + (b.R - a.R) * t),
          (byte)(a.G + (b.G - a.G) * t),
          (byte)(a.B + (b.B - a.B) * t));
    }
  }
}
