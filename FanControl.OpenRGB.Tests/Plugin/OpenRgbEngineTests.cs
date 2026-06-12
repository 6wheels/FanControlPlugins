using FanControl.OpenRGB;
using FanControl.OpenRGB.Effects;
using FanControl.OpenRGB.Rules;
using FanControl.OpenRGB.Toolkit;
using Xunit;

namespace FanControl.OpenRGB.Tests.Plugin;

public class OpenRgbEngineTests
{
    static FakeBroker BrokerWith(params string[] names)
        => new() { Devices = names.Select(n => DeviceBuilder.MakeDevice(n, 1)).ToArray() };

    static RuleBinding Binding(string deviceRegex, float threshold, float value)
    {
        var config = new RuleConfig
        {
            DeviceRegex = deviceRegex,
            ActivationThreshold = threshold,
            Effect = new StaticEffect { ColorHex = "#FF0000", ModulateByValue = false }
        };
        var control = new OpenRgbControlSensor("id", "test");
        control.Set(value);
        return new RuleBinding(config, control);
    }

    static OpenRgbEngine Engine(
        Func<OpenRgbConfig, IOpenRgbBroker> connect,
        OpenRgbConfig? config = null,
        FakeTimeProvider? time = null)
        => new(config ?? new OpenRgbConfig(), connect, (_, _) => { }, time ?? new FakeTimeProvider());

    // --- connect / happy path ---

    [Fact]
    public void Connect_NoStartup_TransitionsToRunning()
    {
        var broker = BrokerWith("GPU");
        var engine = Engine(_ => broker);

        engine.Begin();
        engine.Tick();

        Assert.Equal(OpenRgbEngine.State.Running, engine.CurrentState);
    }

    [Fact]
    public void Running_AppliesMatchingBinding()
    {
        var broker = BrokerWith("GPU");
        var engine = Engine(_ => broker);
        engine.SetBindings([Binding("GPU", threshold: 0f, value: 100f)]);

        engine.Begin();
        engine.Tick(); // Connecting -> Running
        engine.Tick(); // Running render

        Assert.Single(broker.UpdateLedsCalls);
        Assert.Equal(0, broker.UpdateLedsCalls[0].Index);
    }

    // --- startup timing (deterministic via FakeTimeProvider) ---

    [Fact]
    public void Startup_StaysUntilDurationElapses_ThenRuns()
    {
        var broker = BrokerWith("GPU");
        var time = new FakeTimeProvider();
        var config = new OpenRgbConfig
        {
            Startup = new StartupConfig { DurationSeconds = 5.0, Effect = new StaticEffect { ColorHex = "#0000FF", ModulateByValue = false } }
        };
        var engine = Engine(_ => broker, config, time);

        engine.Begin();
        engine.Tick(); // Connecting -> Startup
        Assert.Equal(OpenRgbEngine.State.Startup, engine.CurrentState);

        engine.Tick(); // within window: renders startup frame, stays Startup
        Assert.Equal(OpenRgbEngine.State.Startup, engine.CurrentState);
        Assert.NotEmpty(broker.UpdateLedsCalls);

        time.Advance(TimeSpan.FromSeconds(6));
        engine.Tick(); // window elapsed -> Running
        Assert.Equal(OpenRgbEngine.State.Running, engine.CurrentState);
    }

    // --- failure / reconnect ---

    [Fact]
    public void ConnectFailure_EntersError()
    {
        var engine = Engine(_ => throw new InvalidOperationException("server down"));

        engine.Begin();
        engine.Tick();

        Assert.Equal(OpenRgbEngine.State.Error, engine.CurrentState);
    }

    [Fact]
    public void Error_BacksOff_ThenRetriesConnect()
    {
        var broker = BrokerWith("GPU");
        int calls = 0;
        var time = new FakeTimeProvider();
        var config = new OpenRgbConfig { Reconnect = new ReconnectConfig { MaxRetries = 5, DelaySeconds = 5 } };
        var engine = Engine(_ =>
        {
            calls++;
            if (calls == 1) throw new InvalidOperationException("down");
            return broker;
        }, config, time);

        engine.Begin();
        engine.Tick(); // connect fails -> Error
        Assert.Equal(OpenRgbEngine.State.Error, engine.CurrentState);

        engine.Tick(); // still backing off
        Assert.Equal(OpenRgbEngine.State.Error, engine.CurrentState);

        time.Advance(TimeSpan.FromSeconds(5));
        engine.Tick(); // backoff elapsed -> Connecting
        Assert.Equal(OpenRgbEngine.State.Connecting, engine.CurrentState);

        engine.Tick(); // reconnect succeeds -> Running
        Assert.Equal(OpenRgbEngine.State.Running, engine.CurrentState);
    }

    [Fact]
    public void Reconnect_Exhausted_TransitionsToFailed()
    {
        var time = new FakeTimeProvider();
        var config = new OpenRgbConfig { Reconnect = new ReconnectConfig { MaxRetries = 2, DelaySeconds = 1 } };
        var engine = Engine(_ => throw new InvalidOperationException("down"), config, time);

        engine.Begin();

        // Drive until terminal, advancing past each backoff window.
        for (int i = 0; i < 20 && engine.CurrentState != OpenRgbEngine.State.Failed; i++)
        {
            engine.Tick();
            time.Advance(TimeSpan.FromSeconds(1));
        }

        Assert.Equal(OpenRgbEngine.State.Failed, engine.CurrentState);
    }

    [Fact]
    public void MaxRetriesZero_FailsWithoutRetrying()
    {
        var time = new FakeTimeProvider();
        var config = new OpenRgbConfig { Reconnect = new ReconnectConfig { MaxRetries = 0, DelaySeconds = 0 } };
        var engine = Engine(_ => throw new InvalidOperationException("down"), config, time);

        engine.Begin();
        engine.Tick(); // connect fails -> Error
        engine.Tick(); // delay 0 + retries 0 -> Failed

        Assert.Equal(OpenRgbEngine.State.Failed, engine.CurrentState);
    }

    [Fact]
    public void ConnectionLost_WhileRunning_EntersError()
    {
        var broker = BrokerWith("GPU");
        var engine = Engine(_ => broker);

        engine.Begin();
        engine.Tick(); // -> Running
        broker.Connected = false;
        engine.Tick(); // detects drop -> Error

        Assert.Equal(OpenRgbEngine.State.Error, engine.CurrentState);
    }

    [Fact]
    public void Failed_IsTerminal()
    {
        var time = new FakeTimeProvider();
        var config = new OpenRgbConfig { Reconnect = new ReconnectConfig { MaxRetries = 0, DelaySeconds = 0 } };
        var engine = Engine(_ => throw new InvalidOperationException("down"), config, time);

        engine.Begin();
        engine.Tick();
        engine.Tick(); // -> Failed
        engine.Tick(); // stays Failed

        Assert.Equal(OpenRgbEngine.State.Failed, engine.CurrentState);
    }

    // --- disposal ---

    [Fact]
    public void Dispose_DisposesBroker()
    {
        var broker = BrokerWith("GPU");
        var engine = Engine(_ => broker);

        engine.Begin();
        engine.Tick(); // -> Running, broker connected

        engine.Dispose();

        Assert.True(broker.Disposed);
    }
}

internal sealed class FakeTimeProvider : TimeProvider
{
    private long _ticks;
    public override long TimestampFrequency => TimeSpan.TicksPerSecond;
    public override long GetTimestamp() => _ticks;
    public void Advance(TimeSpan delta) => _ticks += delta.Ticks;
}
