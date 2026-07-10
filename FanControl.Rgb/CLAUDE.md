# Plugin: RGB (FanControl.Rgb)
Drives RGB effects in reaction to FanControl events through two sinks: OpenRGB
(frame sink) for most hardware, and the LiquidCtl bridge (command sink) for NZXT
devices.

## Runtime dependency
- Requires OpenRGB running with its SDK server enabled (`localhost:6742`).
- The debug harness connects to the same endpoint to inspect and replay data.
- Config file `RGBConfig.json` is read from the plugin's own folder
  (`…\Plugins\FanControl.Rgb\`), not the FanControl application directory.

## NZXT command sink
- NZXT RGB is pushed through the LiquidCtl bridge's `LiquidCtlPipeRgb` named pipe
  (`set.led`), so it shares the single serialized HID queue with the fan commands.
  `NzxtBridge` is a pure client — it must NEVER spawn or kill the bridge process
  (the LiquidCtl plugin owns that lifecycle).
- Requires a LiquidCtl build exposing the RGB pipe. Shipped upstream in
  `antoine-bouteiller/FanControl.LiquidCtl` >= v2.5.0 (no fork needed). The bridge
  applies per-LED colour frames only (no firmware effect trigger).
- OpenRGB.exe must have the NZXT devices disabled/blacklisted so liquidctl is the
  sole owner of that HID — otherwise the USB contention returns.

## Resource & error handling
- OpenRGB connections are unmanaged: dispose them via `IDisposable`/`using`.
- Wrap every OpenRGB call in `try-catch`; degrade gracefully if the server is absent.
- The NZXT bridge degrades silently when the pipe is absent and retries later.
