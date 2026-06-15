using System.Diagnostics.CodeAnalysis;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using OpenRGB.NET;

namespace FanControl.OpenRGB.Toolkit;

// Pure client of the LiquidCtl bridge pipe. It NEVER spawns or kills the bridge
// process — the LiquidCtl plugin owns that lifecycle (and kills stray bridge
// processes on init). We only connect as a second client and push RGB commands;
// if the bridge is absent we degrade silently and retry on the next frame.
[ExcludeFromCodeCoverage] // touches a real named pipe; covered via INzxtBridge fakes
internal sealed class NzxtBridge : INzxtBridge
{
    private const int ConnectTimeoutMs = 500;

    private readonly string _pipeName;
    private readonly Action<string, LogLevel> _log;
    private readonly object _lock = new();
    private NamedPipeClientStream? _pipe;
    private bool _disposed;

    public NzxtBridge(string pipeName, Action<string, LogLevel> log)
    {
        _pipeName = pipeName;
        _log = log;
    }

    public bool Connected
    {
        get { lock (_lock) return _pipe is { IsConnected: true }; }
    }

    public void SetLeds(string deviceMatch, string channel, Color[] colors)
    {
        var data = new
        {
            Device = deviceMatch,
            Channel = channel,
            Mode = "super-fixed",
            Colors = Array.ConvertAll(colors, c => new[] { c.R, c.G, c.B })
        };
        Send(new { Command = "set.led", Data = data });
    }

    private void Send(object request)
    {
        if (_disposed) return;

        lock (_lock)
        {
            if (!EnsureConnected()) return;

            try
            {
                byte[] payload = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(request));
                _pipe!.Write(payload, 0, payload.Length);
                _pipe.Flush();

                // Drain the bridge's ack so the next request reads a clean message.
                byte[] buffer = new byte[4096];
                _pipe.Read(buffer, 0, buffer.Length);
            }
            catch (Exception ex) when (ex is IOException or TimeoutException or InvalidOperationException or ObjectDisposedException)
            {
                _log($"NZXT bridge request failed: {ex.Message}", LogLevel.Warning);
                _pipe?.Dispose();
                _pipe = null;
            }
        }
    }

    private bool EnsureConnected()
    {
        if (_pipe is { IsConnected: true }) return true;

        try
        {
            _pipe?.Dispose();
            _pipe = new NamedPipeClientStream(".", _pipeName, PipeDirection.InOut, PipeOptions.Asynchronous);
            _pipe.Connect(ConnectTimeoutMs);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                _pipe.ReadMode = PipeTransmissionMode.Message;
            return true;
        }
        catch (Exception ex) when (ex is IOException or TimeoutException or OperationCanceledException)
        {
            // Bridge not up yet (LiquidCtl plugin not loaded, or starting). Retry later.
            _pipe?.Dispose();
            _pipe = null;
            return false;
        }
    }

    public void Dispose()
    {
        _disposed = true;
        lock (_lock)
        {
            _pipe?.Dispose();
            _pipe = null;
        }
    }
}
