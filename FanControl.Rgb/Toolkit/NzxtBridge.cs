using System.Diagnostics.CodeAnalysis;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using OpenRGB.NET;

namespace FanControl.Rgb.Toolkit;

// Pure client of the LiquidCtl bridge pipe. It NEVER spawns or kills the bridge
// process — the LiquidCtl plugin owns that lifecycle (and kills stray bridge
// processes on init). We only connect as a second client and push RGB commands;
// if the bridge is absent we degrade silently and retry on the next frame.
[ExcludeFromCodeCoverage] // touches a real named pipe; covered via INzxtBridge fakes
internal sealed class NzxtBridge : INzxtBridge
{
    private const int ConnectTimeoutMs = 500;

    // The bridge decodes lower-case JSON keys (msgspec structs: command, data,
    // device, channel, mode, colors).
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly string _pipeName;
    private readonly Action<string, LogLevel> _log;
    private readonly object _lock = new();
    private NamedPipeClientStream? _pipe;
    private bool _disposed;
    private bool _wasConnected;
    private bool _loggedConnectFailure;

    public NzxtBridge(string pipeName, Action<string, LogLevel> log)
    {
        _pipeName = pipeName;
        _log = log;
    }

    public bool Connected
    {
        get { lock (_lock) return _pipe is { IsConnected: true }; }
    }

    public bool SetLeds(string deviceMatch, string channel, Color[] colors)
    {
        var data = new
        {
            Device = deviceMatch,
            Channel = channel,
            Mode = "super-fixed",
            Colors = EncodeColors(colors)
        };
        return Send(new { Command = "set.led", Data = data });
    }

    // Per-LED [r,g,b]. Must be int[], not byte[]: System.Text.Json serializes
    // byte[] as a base64 string, which the bridge rejects (expects an array).
    internal static int[][] EncodeColors(Color[] colors)
        => Array.ConvertAll(colors, c => new[] { (int)c.R, (int)c.G, (int)c.B });

    private bool Send(object request)
    {
        if (_disposed) return false;

        lock (_lock)
        {
            if (!EnsureConnected()) return false;

            try
            {
                string json = JsonSerializer.Serialize(request, JsonOptions);
                _log($"NZXT TX -> {_pipeName}: {json}", LogLevel.Debug);

                byte[] payload = Encoding.UTF8.GetBytes(json);
                _pipe!.Write(payload, 0, payload.Length);
                _pipe.Flush();

                // Read the bridge's response so we can see whether it accepted the
                // frame; also keeps the stream clean for the next request.
                byte[] buffer = new byte[4096];
                int read = _pipe.Read(buffer, 0, buffer.Length);
                _log($"NZXT RX <- ({read} bytes): {Encoding.UTF8.GetString(buffer, 0, read)}", LogLevel.Debug);
                return true;
            }
            catch (Exception ex) when (ex is IOException or TimeoutException or InvalidOperationException or ObjectDisposedException)
            {
                _log($"NZXT bridge request failed: {ex.Message}", LogLevel.Warning);
                _pipe?.Dispose();
                _pipe = null;
                return false;
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
            if (!_wasConnected)
                _log($"NZXT bridge connected on pipe '{_pipeName}'.", LogLevel.Info);
            _wasConnected = true;
            _loggedConnectFailure = false;
            return true;
        }
        catch (Exception ex) when (ex is IOException or TimeoutException or OperationCanceledException)
        {
            // Bridge not up yet (LiquidCtl plugin not loaded, or starting). Retry later.
            _pipe?.Dispose();
            _pipe = null;
            _wasConnected = false;
            if (!_loggedConnectFailure)
            {
                _log($"NZXT bridge pipe '{_pipeName}' not reachable yet ({ex.Message}); retrying.", LogLevel.Warning);
                _loggedConnectFailure = true;
            }
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
