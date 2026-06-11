# Plugin: OpenRGB
Drives RGB effects in OpenRGB in reaction to FanControl events.

## Runtime dependency
- Requires OpenRGB running with its SDK server enabled (`localhost:6742`).
- The debug harness connects to the same endpoint to inspect and replay data.

## Resource & error handling
- OpenRGB connections are unmanaged: dispose them via `IDisposable`/`using`.
- Wrap every OpenRGB call in `try-catch`; degrade gracefully if the server is absent.
