using FanControl.OpenRGB.Rules;
using OpenRGB.NET;

namespace FanControl.OpenRGB.Toolkit;

// Owns the OpenRGB runtime: connection lifecycle, devices, frame buffers, the
// render timer, and a state machine driving startup → running, with bounded
// reconnection on failure. The plugin is only an orchestrator around this.
//
// Dependencies are injected so the whole machine is testable without a real
// server or wall-clock: a connect factory and a TimeProvider.
internal sealed class OpenRgbEngine : IDisposable
{
    internal enum State { Connecting, Startup, Running, Error, Failed, Stopped }

    private readonly OpenRgbConfig _config;
    private readonly Func<OpenRgbConfig, IOpenRgbBroker> _connect;
    private readonly Action<string, LogLevel> _log;
    private readonly TimeProvider _time;
    private readonly Func<bool> _isSuspended;

    private IOpenRgbBroker? _broker;
    private Device[] _devices = [];
    private Color[][] _buffers = [];
    private bool[] _deviceNeedsUpdate = [];
    private int _frameCount;
    private int _retryCount;

    private long _startupStamp; // timestamp the startup animation began
    private long _backoffStamp; // timestamp the current backoff began

    private volatile IReadOnlyList<RuleBinding> _bindings = [];

    private Timer? _timer;
    private volatile bool _isTicking;
    private State _state = State.Stopped;

    internal State CurrentState => _state;

    public OpenRgbEngine(
        OpenRgbConfig config,
        Func<OpenRgbConfig, IOpenRgbBroker> connect,
        Action<string, LogLevel> log,
        TimeProvider? timeProvider = null,
        Func<bool>? isSuspended = null)
    {
        _config = config;
        _connect = connect;
        _log = log;
        _time = timeProvider ?? TimeProvider.System;
        // DevToolkit takeover gate. Injectable so tests don't depend on a
        // process-global temp file.
        _isSuspended = isSuspended ?? (() => File.Exists(LockFile.Path));
    }

    // Bindings are swapped atomically (new list each time) so a concurrent tick
    // never iterates a list being mutated by Load().
    public void SetBindings(IReadOnlyList<RuleBinding> bindings) => _bindings = bindings;

    public void Start()
    {
        Begin();
        int interval = 1000 / _config.Framerate;
        _timer = new Timer(_ => Tick(), null, 0, interval);
    }

    // Arms the state machine without a timer. Test seam: lets tests drive Tick()
    // deterministically instead of relying on the threadpool timer.
    internal void Begin() => _state = State.Connecting;

    // Advances the state machine by one step. Internal (not private) so tests
    // can drive it deterministically without the timer.
    internal void Tick()
    {
        if (_isTicking) return; // reentrancy guard: USB commits can be slow
        _isTicking = true;
        try
        {
            if (_isSuspended()) return; // DevToolkit takeover gate
            _state = Advance(_state);
        }
        catch (Exception ex)
        {
            // Last-resort guard so nothing escapes the timer thread (an unhandled
            // exception here terminates the host). Stay in the current state;
            // handlers own their own recovery/backoff. Do NOT force a reconnect
            // from here — a transient hiccup must not tear down the connection.
            _log($"Engine tick failed: {ex.Message}", LogLevel.Error);
        }
        finally
        {
            _isTicking = false;
        }
    }

    private State Advance(State s) => s switch
    {
        State.Connecting => HandleConnecting(),
        State.Startup => HandleStartup(),
        State.Running => HandleRunning(),
        State.Error => HandleError(),
        _ => s, // Failed, Stopped: terminal / idle
    };

    private State HandleConnecting()
    {
        try
        {
            _broker = _connect(_config);
            _devices = _broker.GetAllControllerData();

            _buffers = new Color[_devices.Length][];
            _deviceNeedsUpdate = new bool[_devices.Length];
            for (int i = 0; i < _devices.Length; i++)
                _buffers[i] = _devices[i].Colors;

            _retryCount = 0;
            _log($"Connected. {_devices.Length} device(s) detected.", LogLevel.Info);

            if (_config.Startup?.Effect != null)
            {
                _startupStamp = _time.GetTimestamp();
                _log($"Startup animation for {_config.Startup.DurationSeconds}s.", LogLevel.Info);
                return State.Startup;
            }
            return State.Running;
        }
        catch (Exception ex)
        {
            // Self-gate the failure into a backed-off Error rather than letting it
            // bubble to the tick guard, which would retry connect every frame.
            _log($"Connection failed: {ex.Message}", LogLevel.Error);
            DisposeBroker();
            return EnterError();
        }
    }

    private State HandleStartup()
    {
        if (_time.GetElapsedTime(_startupStamp).TotalSeconds < _config.Startup!.DurationSeconds)
        {
            RenderStartupFrame(BuildContext(), _frameCount++);
            return State.Startup;
        }
        return State.Running; // startup elapsed, hand over to the layer stack
    }

    private State HandleRunning()
    {
        try
        {
            RenderLayers(BuildContext(), _frameCount++);
            return State.Running;
        }
        catch (Exception ex)
        {
            // Only a genuine connection loss should tear down and reconnect.
            // A transient render hiccup while still connected is logged and the
            // frame skipped — keep rendering, never spin the reconnect loop.
            if (_broker == null || !_broker.Connected)
            {
                _log($"Connection lost: {ex.Message}", LogLevel.Error);
                DisposeBroker();
                return EnterError();
            }
            _log($"Render frame failed: {ex.Message}", LogLevel.Error);
            return State.Running;
        }
    }

    private State HandleError()
    {
        if (_time.GetElapsedTime(_backoffStamp).TotalSeconds < _config.Reconnect.DelaySeconds)
            return State.Error; // still backing off

        if (_retryCount >= _config.Reconnect.MaxRetries)
        {
            _log($"Reconnect exhausted after {_retryCount} attempt(s). Engine stopped driving LEDs.", LogLevel.Error);
            return State.Failed;
        }

        _retryCount++;
        _log($"Reconnect attempt {_retryCount}/{_config.Reconnect.MaxRetries}.", LogLevel.Warning);
        DisposeBroker();
        return State.Connecting;
    }

    private State EnterError()
    {
        _backoffStamp = _time.GetTimestamp();
        return State.Error;
    }

    private RenderContext BuildContext() =>
        new(_broker!, _devices, _buffers, _deviceNeedsUpdate, _bindings, _config);

    // --- Pure render helpers (effect logic untouched, kept static + testable) ---

    // Startup owns the whole frame: apply across all devices and push every one.
    internal static void RenderStartupFrame(in RenderContext ctx, int frameCount)
    {
        ctx.Config.Startup!.Effect.Apply(ctx.Devices, ".*", null, null, 100f, frameCount, ctx.Config.TransitionSpeed, ctx.Buffers);
        for (int i = 0; i < ctx.Devices.Length; i++)
            ctx.Broker.UpdateLeds(i, ctx.Buffers[i]);
    }

    // Applies each active rule's effect into the shared buffers, then commits
    // only the devices a layer actually targeted.
    internal static void RenderLayers(in RenderContext ctx, int frameCount)
    {
        Array.Clear(ctx.DeviceNeedsUpdate);

        foreach (var binding in ctx.Bindings)
        {
            float val = binding.Control.Value ?? 0f;
            if (val < binding.Config.ActivationThreshold) continue;

            // Re-scale so effects always receive 0–100 relative to the activation range,
            // not the raw sensor value. A threshold of 75 means raw=75 → effect sees 0, raw=100 → 100.
            float range = 100f - binding.Config.ActivationThreshold;
            float valueToPass = range > 0 ? Math.Clamp(((val - binding.Config.ActivationThreshold) / range) * 100f, 0f, 100f) : 100f;

            float speedToUse = binding.Config.TransitionSpeed ?? ctx.Config.TransitionSpeed;

            binding.Config.Effect?.Apply(
                ctx.Devices,
                binding.Config.DeviceRegex,
                binding.Config.ZoneRegex,
                binding.Config.LedRegex,
                valueToPass,
                frameCount,
                speedToUse,
                ctx.Buffers
            );

            for (int i = 0; i < ctx.Devices.Length; i++)
            {
                if (binding.DeviceRegex.IsMatch(ctx.Devices[i].Name ?? ""))
                {
                    ctx.DeviceNeedsUpdate[i] = true;
                }
            }
        }

        // Only push to USB if a layer was targeting this device.
        for (int i = 0; i < ctx.Devices.Length; i++)
        {
            if (ctx.DeviceNeedsUpdate[i])
            {
                ctx.Broker.UpdateLeds(i, ctx.Buffers[i]);
            }
        }
    }

    private void DisposeBroker()
    {
        _broker?.Dispose();
        _broker = null;
    }

    public void Dispose()
    {
        // Drain any in-flight tick before disposing the broker, otherwise a
        // running render could touch a disposed OpenRGB connection.
        if (_timer != null)
        {
            using var drained = new ManualResetEvent(false);
            _timer.Dispose(drained);
            drained.WaitOne();
            _timer = null;
        }
        DisposeBroker();
        _state = State.Stopped;
    }
}
