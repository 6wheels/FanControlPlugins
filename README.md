# FanControl Plugins Collection

This repository is a monorepo containing custom plugins for [FanControl](https://github.com/Rem0o/FanControl.Releases), a highly customizable fan management software for Windows. 

The build and deployment process is fully automated. Every new release is built via GitHub Actions, and the compiled `.dll` files are directly available on the Releases page.

## Available Plugins

* **[FanControl.OpenRGB](./FanControl.OpenRGB/README.md)**: Bridges FanControl and OpenRGB, allowing hardware temperatures and custom fan curves to drive RGB lighting dynamically (supports 2D Matrix keyboards, startup animations, and threshold blinking).
* *(Plugin 2 - Placeholder)*: Description...
* *(Plugin 3 - Placeholder)*: Description...

## 📦 Installation for Users

You do **not** need to compile the source code to use these plugins.

1. Go to the [Releases page](../../releases) of this repository.
2. Download the `.dll` file of the plugin you want.
3. Install through the FanControl interface or place the downloaded `.dll` inside your FanControl `Plugins` folder (e.g., `C:\Program Files (x86)\FanControl\Plugins`).
4. Restart FanControl. Make sure plugins are enabled in the FanControl settings.

---

## 🛠️ Prerequisites for Development

If you want to contribute or build the plugins locally from the source code, please follow these steps:

### 1. Environment
* [.NET 8.0 SDK](https://dotnet.microsoft.com/download)
* Visual Studio 2022, JetBrains Rider, or VS Code.
* FanControl installed locally on your Windows machine.

### 2. The `lib` Folder Setup
To respect copyright and avoid binary bloat, the core FanControl API library is **not** included in this repository and is ignored by Git. You must provide it locally for the solution to compile.

Thanks to the `Directory.Build.props` architecture, you only need to do this once for all plugins:

1. Create a folder named `lib` at the root of the cloned repository (next to the `.sln` file).
2. Go to your local FanControl installation folder.
3. Copy the file `FanControl.Plugins.dll`.
4. Paste it inside the newly created `lib/` folder.

Your tree should look like this:
```text
/lib/
  └── FanControl.Plugins.dll
/FanControl.OpenRGB/
/FanControl.Plugin2/
Directory.Build.props
FanControl.Plugins.sln
```

### 3. Build
Simply build the solution in Release mode.
Dependencies (like OpenRGB.NET) are automatically bundled into a single output .dll using Costura.Fody.
