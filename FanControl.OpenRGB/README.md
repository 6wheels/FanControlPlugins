# FanControl.OpenRGB

[![Build](https://github.com/6wheels/FanControlPlugins/actions/workflows/release.yml/badge.svg)](https://github.com/6wheels/FanControlPlugins/actions/workflows/release.yml) [![Coverage](https://codecov.io/gh/6wheels/FanControlPlugins/branch/main/graph/badge.svg?flag=FanControl.OpenRGB)](https://codecov.io/gh/6wheels/FanControlPlugins)

A FanControl plugin that turns your hardware cooling logic into dynamic RGB lighting effects. By communicating directly with the OpenRGB SDK, this plugin maps any FanControl sensor (temperature, fan curve, mix) to specific RGB zones or devices.

## Features
* **Regex Targeting:** Target specific devices (e.g., `.*Alloy.*`) or zones (e.g., `Keyboard`) effortlessly.
* **2D Matrix Support:** Advanced spatial effects like Aurora Borealis aware of horizontal/vertical hardware layouts.
* **Startup Animations:** Non-blocking boot sequences (e.g., a 5-second wave effect before switching to temperature monitoring).
* **Hardware Safety:** Strictly limits update rates to prevent USB controller flooding.
* **Resilient Connection:** If the OpenRGB server starts late or drops mid-session, the plugin reconnects automatically with bounded retries instead of staying dead.

## ⚙️ Prerequisites

1.  **[OpenRGB](https://openrgb.org/)** must be installed and running.
2.  In the OpenRGB UI, navigate to the **SDK Server** tab.
3.  Set the Server Port to **6742**.
4.  Check **Auto Start Server**.
5.  *(Recommended)* Check **Start at login** and **Start minimized** in the General Settings.

## 📝 Configuration

Upon the first launch, the plugin creates an `OpenRGBConfig.json` file in your FanControl folder. You can configure global behaviors, the startup animation, and your hardware rules.

### Example `OpenRGBConfig.json`

```json
{
  "ServerIp": "127.0.0.1",
  "ServerPort": 6742,
  "Framerate": 30,
  "LogLevel": "Info",
  "Reconnect": {
    "MaxRetries": 5,
    "DelaySeconds": 5
  },
  "Startup": {
    "DurationSeconds": 4.5,
    "Effect": {
      "Type": "Aurora",
      "Direction": "Horizontal",
      "Color1Hex": "#00FF66",
      "Color2Hex": "#00FFFF",
      "Color3Hex": "#9900FF",
      "Speed": 0.08,
      "Scale": 0.4
    }
  },
  "Rules": [
    {
      "Id": "rgb_gpu_warning",
      "Name": "GPU High Temp Warning",
      "DeviceRegex": ".*Alloy.*",
      "ActivationThreshold": 75.0,
      "Effect": {
        "Type": "Blink",
        "Color1Hex": "#FF0000",
        "Color2Hex": "#000000",
        "SlowBlinkHz": 1.0,
        "FastBlinkHz": 10.0,
        "ModulateByValue": true
      }
    }
  ]
}
```

### Connection & Reconnect
The plugin drives RGB through a small state machine: it connects to the OpenRGB SDK server, optionally plays the startup animation, then renders your rules. If the server is unavailable at launch or the connection drops later, it enters a bounded reconnect loop.

- `Reconnect.MaxRetries` — how many reconnect attempts before giving up (terminal). Default `5`.
- `Reconnect.DelaySeconds` — delay between attempts. Default `5`.

Once retries are exhausted the engine stops driving LEDs and logs; restart FanControl (or fix the server) to recover. Transient render errors while still connected are skipped without tearing down the connection.

### Understanding Rules
Once the plugin loads the JSON, you will see a new custom sensor card in FanControl for each rule (e.g., "GPU High Temp Warning").

- Assign a curve or temperature to this card in the FanControl UI.
- When the card's value reaches the `ActivationThreshold` (e.g., 75%), the rule's `Effect` is applied to the matching devices.
- Below the threshold the rule is inactive and leaves those LEDs untouched, so a lower-threshold rule (or OpenRGB itself) can control them.
- The effect receives a value re-scaled to `0–100` across the activation range, so `ModulateByValue` effects ramp from the threshold up to 100%.

### Effects Types
- `Static`: Requires `ColorHex`. Displays a solid color, optionally dimmed by the current value when `ModulateByValue` is true.
- `Gradient`: Requires `ColorMinHex` and `ColorMaxHex`. Colors interpolate between minimum and maximum values based on the current value.
- `Blink`: Requires `Color1Hex` and `Color2Hex`. `SlowBlinkHz` and `FastBlinkHz` set the blink frequency range (in Hz); when `ModulateByValue` is true, the frequency scales from slow (low value) to fast (high value).
- `Breathing`: Requires `BaseColorHex`, `PeakColorHex`, `MinSpeed`, and `MaxSpeed`. Creates a smooth pulsating fade whose speed increases with the current value.
- `Aurora`: Requires `Color1Hex`, `Color2Hex`, `Color3Hex`, `Speed`, `Scale`, and `Direction` (`Horizontal` or `Vertical`). Produces a moving band effect that respects 2D matrix layouts when available.
- `SpatialGradient`: Requires `ColorMinHex` and `ColorMaxHex`. Draws a left-to-right gradient across a 1D strip or 2D matrix.
- `GaugeGradient`: Requires `ColorMinHex` and `ColorMaxHex`. At value `0`, all selected LEDs are `ColorMinHex`. As the value increases, the gradient fills spatially toward `ColorMaxHex`.
- `ProgressBar`: Requires `FillColorHex` and optional `EmptyColorHex`. Lights LEDs sequentially to represent the current value, with an optional transparent empty state.

## 📚 Dependencies

This project uses:
- **[OpenRGB.NET](https://github.com/diogotr7/OpenRGB.NET) 3.1.1** — MIT license — embedded into the output `.dll` via Costura.Fody (bundling permitted by MIT).
- **[FanControl.Plugins](https://github.com/Rem0o/FanControl.Releases)** — the host plugin API (proprietary; supplied by FanControl at runtime, **not** redistributed).

## 🧰 The Developer Toolkit (Standalone Mode)

This project has a unique architecture: while it compiles into a `.dll` to be loaded as a FanControl plugin, it also contains a `Main()` entry point. This allows it to be run directly as a standalone Console Application for debugging hardware and testing effects dynamically.

### How to run the Toolkit
There are two ways to launch the Dev Toolkit:

**Method 1: Direct Execution (Compiled)**
If you built the project in Visual Studio, simply double-click the `FanControl.OpenRGB.exe` generated in your `bin/Release/` or `bin/Debug/` folder.

**Method 2: Dotnet CLI (Source)**
Open a terminal in the `FanControl.OpenRGB` directory and run:
`dotnet.exe run`

### Features of the Toolkit
When launched, the Dev Toolkit automatically creates a `fancontrol_rgb.lock` file. This tells the FanControl background plugin to pause its rendering loop, preventing USB port conflicts.
1. **Hardware Scanner:** Scans your setup and prints OpenRGB device, zone, and LED details, including matrix dimensions when available. This output is useful for constructing `DeviceRegex`, `ZoneRegex`, and `LEDRegex` filters.
2. **Effects Tester:** Automatically discovers effect classes with reflection. You can enter custom effect parameters at runtime and test them immediately.

### Toolkit Usage Notes
- Leave `Device filter regex` blank or type `.*` to target all devices.
- Leave `Zone filter regex` and `LED filter regex` blank to target every matching LED.
- For the simulated sensor value, leave the field blank or type `auto` to enable the default auto-oscillating mode.
- If you enter a numeric value, the toolkit uses manual mode and lets you adjust the value with `+` / `-` without exiting the effect.
- Press `ESC` any time to stop the running effect and return to the toolkit menu.
