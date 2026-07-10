using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using FanControl.Rgb.Toolkit.Rendering;
using OpenRGB.NET;

namespace FanControl.Rgb.Effects
{
  [JsonPolymorphic(TypeDiscriminatorPropertyName = "Type")]
  [JsonDerivedType(typeof(StaticEffect), "Static")]
  [JsonDerivedType(typeof(GradientEffect), "Gradient")]
  [JsonDerivedType(typeof(BlinkEffect), "Blink")]
  [JsonDerivedType(typeof(BreathingEffect), "Breathing")]
  [JsonDerivedType(typeof(AuroraEffect), "Aurora")]
  [JsonDerivedType(typeof(SpatialGradientEffect), "SpatialGradient")]
  [JsonDerivedType(typeof(ProgressBarEffect), "ProgressBar")]
  [JsonDerivedType(typeof(GaugeGradientEffect), "GaugeGradient")]
  [JsonDerivedType(typeof(RainbowEffect), "Rainbow")]
  /// <summary>
  /// Base class for all RGB effects used by the FanControl OpenRGB plugin and the developer toolkit.
  /// </summary>
  public abstract class BaseRgbEffect
  {
    /// <summary>
    /// Cycles-per-second that a speed multiplier of 1.0 maps to. Anchors all animated
    /// effects to one smooth reference rate (one cycle every 4s) so a default Speed of 1.0
    /// looks pleasant out of the box and 2.0 is simply "twice as fast". Motion stays
    /// framerate-independent because effects derive seconds from the sink framerate.
    /// </summary>
    protected const float ReferenceCyclesPerSecond = 0.25f;

    /// <summary>
    /// Framerate the per-frame fade (TransitionSpeed) is authored against. Fades are
    /// normalized to this rate so the same config smooths over the same wall-clock time on
    /// any sink — otherwise the low-rate NZXT sink would lag far behind a moving target and
    /// wash the effect out. At this framerate the normalization is a no-op.
    /// </summary>
    protected const int ReferenceFramerate = 30;

    /// <summary>
    /// Rescales a per-frame fade factor so it reaches the target over the same wall-clock
    /// time regardless of the sink framerate. Snap (>=1) and no-op (&lt;=0) pass through.
    /// </summary>
    protected static float NormalizeFade(float transitionSpeed, int framerate)
    {
      float t = Math.Clamp(transitionSpeed, 0f, 1f);
      if (t <= 0f || t >= 1f || framerate <= 0 || framerate == ReferenceFramerate) return t;
      double exponent = ReferenceFramerate / (double)framerate;
      return (float)(1.0 - Math.Pow(1.0 - t, exponent));
    }

    /// <summary>
    /// If true, the effect will use the sensor value (0-100) as a modulation factor.
    /// </summary>
    public bool ModulateByValue { get; set; } = true;

    [JsonIgnore]
    public bool IsFinished { get; protected set; } = false;

    /// <summary>
    /// Runs the effect on every device whose name matches the configured device regex.
    /// <paramref name="framerate"/> is the sink's real frames-per-second, letting effects
    /// convert <paramref name="frameCount"/> to seconds so speeds are wall-clock accurate.
    /// </summary>
    public void Apply(IRgbDevice[] devices, string deviceRegex, string? zoneRegex, string? ledRegex, float currentValue, int frameCount, int framerate, float transitionSpeed, Color[][] frameBuffers)
    {
      if (IsFinished) return;
      for (int i = 0; i < devices.Length; i++)
      {
        var device = devices[i];
        if (Regex.IsMatch(device.Name ?? "", deviceRegex))
        {
          ProcessEffect(device, zoneRegex, ledRegex, currentValue, frameCount, framerate, transitionSpeed, frameBuffers[i]);
        }
      }
    }

    protected abstract void ProcessEffect(IRgbDevice device, string? zoneRegex, string? ledRegex, float value, int frameCount, int framerate, float transitionSpeed, Color[] buffer);

    protected static void ApplyToTargetLeds(IRgbDevice device, string? zoneRegex, string? ledRegex, Color[] currentColors, Color targetColor, float fadeSpeed = 1.0f)
    {
      static Color LerpColor(Color current, Color target, float speed)
      {
        if (speed <= 0f || speed >= 1.0f) return target;

        byte r = (byte)(current.R + (target.R - current.R) * speed);
        byte g = (byte)(current.G + (target.G - current.G) * speed);
        byte b = (byte)(current.B + (target.B - current.B) * speed);
        return new Color(r, g, b);
      }

      int ledOffset = 0;
      foreach (var zone in device.Zones)
      {
        if (string.IsNullOrEmpty(zoneRegex) || Regex.IsMatch(zone.Name, zoneRegex))
        {
          for (int l = 0; l < zone.LedCount; l++)
          {
            string ledName = device.Leds[ledOffset + l].Name;
            if (string.IsNullOrEmpty(ledRegex) || Regex.IsMatch(ledName, ledRegex))
            {
              currentColors[ledOffset + l] = LerpColor(currentColors[ledOffset + l], targetColor, fadeSpeed);
            }
          }
        }
        ledOffset += (int)zone.LedCount;
      }
    }

    protected static Color LerpColor(Color a, Color b, float t)
    {
      t = Math.Clamp(t, 0f, 1f);
      return new Color((byte)(a.R + (b.R - a.R) * t), (byte)(a.G + (b.G - a.G) * t), (byte)(a.B + (b.B - a.B) * t));
    }

    protected static Color ParseHex(string hex)
    {
      if (string.IsNullOrEmpty(hex)) return new Color(255, 255, 255);

      hex = hex.Replace("#", "");
      if (hex.Length != 6) return new Color(255, 255, 255);

      return new Color(
          Convert.ToByte(hex[..2], 16),
          Convert.ToByte(hex.Substring(2, 2), 16),
          Convert.ToByte(hex.Substring(4, 2), 16)
      );
    }

    /// <summary>
    /// Converts an HSV triple to RGB. h in [0,360), s and v in [0,1].
    /// Shared by hue-cycling effects (Rainbow, Aurora).
    /// </summary>
    protected static Color HsvToRgb(float h, float s, float v)
    {
      h = ((h % 360f) + 360f) % 360f;
      s = Math.Clamp(s, 0f, 1f);
      v = Math.Clamp(v, 0f, 1f);

      float sector = h / 60f;
      int i = (int)Math.Floor(sector) % 6;
      float f = sector - (float)Math.Floor(sector);
      float p = v * (1f - s);
      float q = v * (1f - f * s);
      float t = v * (1f - (1f - f) * s);
      return i switch
      {
        0 => new Color((byte)(v * 255), (byte)(t * 255), (byte)(p * 255)),
        1 => new Color((byte)(q * 255), (byte)(v * 255), (byte)(p * 255)),
        2 => new Color((byte)(p * 255), (byte)(v * 255), (byte)(t * 255)),
        3 => new Color((byte)(p * 255), (byte)(q * 255), (byte)(v * 255)),
        4 => new Color((byte)(t * 255), (byte)(p * 255), (byte)(v * 255)),
        _ => new Color((byte)(v * 255), (byte)(p * 255), (byte)(q * 255)),
      };
    }

    /// <summary>
    /// Rescales a color's saturation via an RGB→HSV→RGB round-trip. saturation in [0,1];
    /// 1.0 keeps the color unchanged, 0.0 collapses it to gray.
    /// </summary>
    protected static Color AdjustSaturation(Color color, float saturation)
    {
      saturation = Math.Clamp(saturation, 0f, 1f);

      float r = color.R / 255f, g = color.G / 255f, b = color.B / 255f;
      float max = Math.Max(r, Math.Max(g, b));
      float min = Math.Min(r, Math.Min(g, b));
      float delta = max - min;
      float v = max;

      float h = 0f;
      if (delta > 1e-6f)
      {
        if (max == r) h = 60f * (((g - b) / delta) % 6f);
        else if (max == g) h = 60f * (((b - r) / delta) + 2f);
        else h = 60f * (((r - g) / delta) + 4f);
      }
      float s = max <= 1e-6f ? 0f : delta / max;

      return HsvToRgb(h, s * saturation, v);
    }

    /// <summary>
    /// Returns the effective gradient stops: the explicit <paramref name="stops"/> list when
    /// non-empty, otherwise a two-stop gradient synthesized from the min/max hex fallback.
    /// </summary>
    protected static IReadOnlyList<GradientStop> ResolveStops(List<GradientStop> stops, string minHex, string maxHex)
    {
      if (stops != null && stops.Count > 0) return stops;
      return new[]
      {
        new GradientStop { Position = 0f, ColorHex = minHex },
        new GradientStop { Position = 1f, ColorHex = maxHex },
      };
    }

    /// <summary>
    /// Samples a multi-stop gradient at <paramref name="position"/> (clamped to [0,1]).
    /// Stops are ordered by Position; the two bracketing stops are linearly interpolated.
    /// An empty list yields white; a single stop yields that stop's color.
    /// </summary>
    protected static Color SampleGradient(IReadOnlyList<GradientStop> stops, float position)
    {
      if (stops == null || stops.Count == 0) return new Color(255, 255, 255);

      position = Math.Clamp(position, 0f, 1f);

      // Copy + sort so the caller's list order never affects the result.
      var ordered = new List<GradientStop>(stops);
      ordered.Sort((a, b) => a.Position.CompareTo(b.Position));

      if (position <= ordered[0].Position) return ParseHex(ordered[0].ColorHex);
      var last = ordered[ordered.Count - 1];
      if (position >= last.Position) return ParseHex(last.ColorHex);

      for (int i = 0; i < ordered.Count - 1; i++)
      {
        GradientStop lo = ordered[i];
        GradientStop hi = ordered[i + 1];
        if (position >= lo.Position && position <= hi.Position)
        {
          float span = hi.Position - lo.Position;
          float t = span <= 1e-6f ? 0f : (position - lo.Position) / span;
          return LerpColor(ParseHex(lo.ColorHex), ParseHex(hi.ColorHex), t);
        }
      }

      return ParseHex(last.ColorHex);
    }
  }
}