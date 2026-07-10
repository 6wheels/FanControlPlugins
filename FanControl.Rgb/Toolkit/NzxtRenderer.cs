using FanControl.Rgb.Rules;
using FanControl.Rgb.Toolkit.Rendering;
using OpenRGB.NET;

namespace FanControl.Rgb.Toolkit;

// Command sink for NZXT devices. Mirrors OpenRgbEngine's rule pass — same
// threshold gate, value rescale, and effect pipeline — but renders onto synthetic
// NZXT devices and flushes per channel through the LiquidCtl bridge. Runs on its
// own low-rate timer (independent of the OpenRGB connection) and only pushes a
// channel when its rendered slice changed, so the NZXT firmware is never flooded.
internal sealed class NzxtRenderer : IDisposable
{
    private readonly NzxtConfig _config;
    private readonly float _defaultTransitionSpeed;
    private readonly INzxtBridge _bridge;
    private readonly Action<string, LogLevel> _log;
    private readonly Func<bool> _isSuspended;
    private readonly StartupConfig? _startup;
    private readonly TimeProvider _time;

    private readonly NzxtDevice[] _devices;
    private readonly IRgbDevice[] _renderDevices;
    private readonly Color[][] _buffers;
    private readonly Color[][] _lastSent;
    private readonly bool[] _primed;
    private readonly bool[] _needsUpdate;

    private volatile IReadOnlyList<RuleBinding> _bindings = [];
    private Timer? _timer;
    private volatile bool _isTicking;
    private long _startStamp; // wall-clock timestamp of the first rendered tick
    private bool _started;

    public NzxtRenderer(
        NzxtConfig config,
        float defaultTransitionSpeed,
        INzxtBridge bridge,
        Action<string, LogLevel> log,
        StartupConfig? startup = null,
        Func<bool>? isSuspended = null,
        TimeProvider? timeProvider = null)
    {
        _config = config;
        _defaultTransitionSpeed = defaultTransitionSpeed;
        _bridge = bridge;
        _log = log;
        _startup = startup?.Effect != null ? startup : null;
        _time = timeProvider ?? TimeProvider.System;
        _isSuspended = isSuspended ?? (() => File.Exists(LockFile.Path));

        _devices = NzxtDeviceFactory.Build(config);
        _renderDevices = Array.ConvertAll(_devices, d => (IRgbDevice)d);
        _buffers = Array.ConvertAll(_devices, d => new Color[d.TotalLeds]);
        _lastSent = Array.ConvertAll(_devices, d => new Color[d.TotalLeds]);
        _primed = new bool[_devices.Length];
        _needsUpdate = new bool[_devices.Length];
    }

    internal int DeviceCount => _devices.Length;

    // Swapped atomically (new list each time) so a concurrent tick never iterates
    // a list being mutated by Load().
    public void SetBindings(IReadOnlyList<RuleBinding> bindings) => _bindings = bindings;

    public void Start()
    {
        if (_devices.Length == 0)
        {
            _log("No NZXT targets configured; command sink idle.", LogLevel.Info);
            return;
        }
        int interval = 1000 / _config.RefreshHz;
        _timer = new Timer(_ => Tick(), null, 0, interval);
        _log($"NZXT command sink started for {_devices.Length} target(s) at {_config.RefreshHz}Hz.", LogLevel.Info);
    }

    // Internal so tests can drive it deterministically without the timer.
    internal void Tick()
    {
        if (_isTicking) return; // reentrancy guard: a pipe round-trip can be slow
        _isTicking = true;
        try
        {
            if (_isSuspended()) return; // DevToolkit takeover gate (shared with the frame sink)
            RenderAndFlush();
        }
        catch (Exception ex)
        {
            _log($"NZXT tick failed: {ex.Message}", LogLevel.Error);
        }
        finally
        {
            _isTicking = false;
        }
    }

    internal void RenderAndFlush()
    {
        Array.Clear(_needsUpdate);

        // Anchor timing to the wall clock, not the tick count: NZXT ticks fire below the
        // nominal RefreshHz (slow pipe round-trips, reentrancy skips), so a frame counter
        // would drift — animations lag and startup overruns. Deriving the frame from real
        // elapsed time keeps effect speed and startup duration matched to the OpenRGB sink.
        if (!_started)
        {
            _startStamp = _time.GetTimestamp();
            _started = true;
        }
        double elapsedSeconds = _time.GetElapsedTime(_startStamp).TotalSeconds;
        int frame = (int)Math.Round(elapsedSeconds * _config.RefreshHz);

        // Startup owns the whole frame: apply across all devices and flush every one,
        // mirroring OpenRgbEngine.RenderStartupFrame. The bridge diff still suppresses
        // unchanged channels, so the firmware is never flooded.
        if (_startup != null && elapsedSeconds < _startup.DurationSeconds)
        {
            _startup.Effect.Apply(_renderDevices, ".*", null, null, 100f, frame, _config.RefreshHz, _defaultTransitionSpeed, _buffers);
            for (int i = 0; i < _devices.Length; i++)
                FlushDevice(i);
            return;
        }

        foreach (var binding in _bindings)
        {
            float val = binding.Control.Value ?? 0f;
            if (val < binding.Config.ActivationThreshold) continue;

            float range = 100f - binding.Config.ActivationThreshold;
            float valueToPass = range > 0
                ? Math.Clamp(((val - binding.Config.ActivationThreshold) / range) * 100f, 0f, 100f)
                : 100f;

            float speed = binding.Config.TransitionSpeed ?? _defaultTransitionSpeed;

            binding.Config.Effect?.Apply(
                _renderDevices,
                binding.Config.DeviceRegex,
                binding.Config.ZoneRegex,
                binding.Config.LedRegex,
                valueToPass,
                frame,
                _config.RefreshHz,
                speed,
                _buffers);

            for (int i = 0; i < _devices.Length; i++)
            {
                if (binding.DeviceRegex.IsMatch(_devices[i].Name ?? ""))
                    _needsUpdate[i] = true;
            }
        }

        for (int i = 0; i < _devices.Length; i++)
        {
            if (_needsUpdate[i])
                FlushDevice(i);
        }
    }

    // Pushes only the channels whose rendered slice changed since the last send
    // (every channel on the first flush, to seed the firmware from a known state).
    private void FlushDevice(int i)
    {
        NzxtDevice device = _devices[i];
        Color[] buffer = _buffers[i];
        Color[] sent = _lastSent[i];
        bool force = !_primed[i];

        // Only record a channel as sent (and prime the device) when the bridge
        // actually accepted the frame. Otherwise a failed first flush — the bridge
        // takes a few seconds to come up — would mark everything sent and the
        // diff would suppress all further frames, leaving the LEDs dark.
        bool allSent = true;

        foreach (NzxtChannelSlice slice in device.Channels)
        {
            if (!force && !SliceChanged(buffer, sent, slice.Offset, slice.Count))
                continue;

            var colors = new Color[slice.Count];
            Array.Copy(buffer, slice.Offset, colors, 0, slice.Count);
            if (_bridge.SetLeds(slice.DeviceMatch, slice.ChannelName, colors))
                Array.Copy(buffer, slice.Offset, sent, slice.Offset, slice.Count);
            else
                allSent = false;
        }

        if (allSent)
            _primed[i] = true;
    }

    private static bool SliceChanged(Color[] current, Color[] sent, int offset, int count)
    {
        for (int k = 0; k < count; k++)
        {
            Color a = current[offset + k];
            Color b = sent[offset + k];
            if (a.R != b.R || a.G != b.G || a.B != b.B) return true;
        }
        return false;
    }

    public void Dispose()
    {
        if (_timer != null)
        {
            using var drained = new ManualResetEvent(false);
            _timer.Dispose(drained);
            drained.WaitOne();
            _timer = null;
        }
        _bridge.Dispose();
    }
}
