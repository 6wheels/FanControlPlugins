namespace FanControl.Rgb.Effects
{
  /// <summary>
  /// A single color stop in a multi-stop gradient. Shared notation reused across gradient
  /// effects (and intended for future built-in NZXT effects).
  /// </summary>
  public sealed class GradientStop
  {
    // Position of the stop along the gradient axis. 0.0-1.0.
    public float Position { get; set; }

    public string ColorHex { get; set; } = "#FFFFFF";
  }
}
