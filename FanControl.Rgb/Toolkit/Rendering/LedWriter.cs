using OpenRGB.NET;

namespace FanControl.Rgb.Toolkit.Rendering
{
  /// <summary>
  /// Centralized per-layer write into a device's frame buffer. Every effect funnels
  /// its LED writes through here instead of touching the buffer directly, so
  /// compositing, opacity and temporal smoothing live in one place (issue #24).
  /// <para>
  /// Step 0 is a pure passthrough: <see cref="Write"/> reproduces the previous inline
  /// <c>buffer[led] = LerpColor(buffer[led], target, fade)</c> exactly. Layer opacity,
  /// black-as-transparent and temporal/layer decoupling are layered in on top later
  /// without touching the effects again.
  /// </para>
  /// Effects that want transparency simply skip the write, mirroring ProgressBar's
  /// empty-transparent model.
  /// </summary>
  public readonly struct LedWriter
  {
    private readonly Color[] _buffer;

    public LedWriter(Color[] buffer) => _buffer = buffer;

    /// <summary>Number of LEDs in the target buffer; used by effects for bounds checks.</summary>
    public int Length => _buffer.Length;

    /// <summary>
    /// Writes <paramref name="target"/> into <paramref name="led"/>, smoothing from the
    /// current buffer value by <paramref name="fade"/> (>=1 snaps, &lt;=0 is a no-op).
    /// </summary>
    public void Write(int led, Color target, float fade)
    {
      _buffer[led] = Lerp(_buffer[led], target, fade);
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
