# FanControl Plugins Collection

This repository is a monorepo containing custom plugins for [FanControl](https://github.com/Rem0o/FanControl.Releases), a highly customizable fan management software for Windows. 

The build and deployment process is fully automated. Every new release is built via GitHub Actions, and each plugin is published as a `.zip` archive (containing the compiled `.dll`) on the Releases page.

## Plugins

| Plugin | Description | Build | Coverage |
| --- | --- | --- | --- |
| [FanControl.OpenRGB](./FanControl.OpenRGB/README.md) | Maps FanControl sensors to OpenRGB lighting effects (static, gradient, blink, aurora, 2D matrix, startup animations). | [![Build](https://img.shields.io/github/actions/workflow/status/6wheels/FanControlPlugins/release.yml?branch=main&label=build&style=flat-square)](https://github.com/6wheels/FanControlPlugins/actions/workflows/release.yml) | [![Coverage](https://img.shields.io/codecov/c/github/6wheels/FanControlPlugins?flag=FanControl.OpenRGB&label=cov&style=flat-square)](https://codecov.io/gh/6wheels/FanControlPlugins) |
| [FanControl.Mqtt](./FanControl.Mqtt/README.md) | Publishes FanControl sensors to an MQTT broker with Home Assistant auto-discovery. | [![Build](https://img.shields.io/github/actions/workflow/status/6wheels/FanControlPlugins/release.yml?branch=main&label=build&style=flat-square)](https://github.com/6wheels/FanControlPlugins/actions/workflows/release.yml) | [![Coverage](https://img.shields.io/codecov/c/github/6wheels/FanControlPlugins?flag=FanControl.Mqtt&label=cov&style=flat-square)](https://codecov.io/gh/6wheels/FanControlPlugins) |
| [FanControl.SystemMetrics](./FanControl.SystemMetrics/README.md) | Exposes Windows performance counters (CPU, GPU, disk) as sensors for activity-driven fan curves. | [![Build](https://img.shields.io/github/actions/workflow/status/6wheels/FanControlPlugins/release.yml?branch=main&label=build&style=flat-square)](https://github.com/6wheels/FanControlPlugins/actions/workflows/release.yml) | [![Coverage](https://img.shields.io/codecov/c/github/6wheels/FanControlPlugins?flag=FanControl.SystemMetrics&label=cov&style=flat-square)](https://codecov.io/gh/6wheels/FanControlPlugins) |

## 📦 Installation for Users

You do **not** need to compile the source code to use these plugins.

1. Go to the [Releases page](../../releases) of this repository.
2. Download the `.zip` archive of the plugin you want and extract the `.dll` from it.
3. Install the `.dll` using either method:
   * **Through the FanControl UI**: open the FanControl settings and import the plugin `.dll`.
   * **Manually**: place the `.dll` inside your FanControl `Plugins` folder (e.g., `C:\Program Files (x86)\FanControl\Plugins`).
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

### 4. Deploy

A `Deploy` MSBuild target stops FanControl, copies the built `.dll` to the plugins folder, and restarts FanControl. It requires a UAC elevation prompt (one per run).

```bash
dotnet.exe build -t:Deploy FanControl.OpenRGB/FanControl.OpenRGB.csproj
```

The default destination is `C:\Program Files (x86)\FanControl\Plugins`. Override it if your install path differs:

```bash
dotnet.exe build -t:Deploy -p:FanControlPluginsDir="C:\your\path\Plugins" FanControl.OpenRGB/FanControl.OpenRGB.csproj
```

### 5. Commit Conventions
This repo uses [Conventional Commits](https://www.conventionalcommits.org/). The commit subject **must** follow:

```text
<type>[(scope)][!]: <description>
```

* **Types**: `feat`, `fix`, `docs`, `style`, `refactor`, `perf`, `test`, `build`, `ci`, `chore`, `revert`.
* **Scope** (optional): usually the plugin, e.g. `feat(openrgb): ...`.
* **Breaking change**: add `!` after the type/scope (e.g. `feat!: ...`) or a `BREAKING CHANGE:` line in the body.

A tracked `commit-msg` hook validates this. Enable it once per clone:

```bash
git config core.hooksPath .githooks
```

Bypass for a single commit with `git commit --no-verify` (merges/reverts/fixups are skipped automatically).

### 6. Releases & Versioning
Versions are **per plugin** and live entirely in git tags — there are no version files to edit.

* **Final release** — push a tag shaped `<Plugin>/v<semver>`, which builds and releases only that plugin at that exact version:
  ```bash
  git tag FanControl.OpenRGB/v1.2.0
  git push origin FanControl.OpenRGB/v1.2.0
  ```
* **Nightly** — every push to `main` (or a manual run) builds all plugins into one rolling `nightly` pre-release. Each artifact previews the plugin's **next** version as `<next>-dev.<short-sha>`, where the bump is derived from the Conventional Commits touching that plugin since its last tag:

  | Commits since the plugin's last tag | Bump |
  | --- | --- |
  | a `feat!:` / `BREAKING CHANGE` | major |
  | a `feat:` | minor |
  | anything else (`fix:`, `refactor:`, …) | patch |

Each release page includes the zipped `.dll` and a changelog scoped to the relevant plugin(s).

---

## 📚 Dependencies & Licenses

Every plugin references the host `FanControl.Plugins` API and bundles its NuGet dependencies into a single `.dll` via Costura.Fody.

| Dependency | Used by | License | Bundled by Costura |
| --- | --- | --- | --- |
| [FanControl.Plugins](https://github.com/Rem0o/FanControl.Releases) | all plugins | Proprietary (Rémi Mercier) | **No** — provided by the FanControl host at runtime, never redistributed (`Private=false`) |
| [Costura.Fody](https://github.com/Fody/Costura) 6.0.0 | all plugins (build only) | MIT | n/a (build-time tool) |
| [OpenRGB.NET](https://github.com/diogotr7/OpenRGB.NET) 3.1.1 | FanControl.OpenRGB | MIT | Yes |
| [MQTTnet](https://github.com/dotnet/MQTTnet) 5.1.0.1559 | FanControl.Mqtt | MIT | Yes |
| [System.Diagnostics.PerformanceCounter](https://github.com/dotnet/runtime) 10.0.8 | FanControl.SystemMetrics | MIT | Yes |

All bundled dependencies are **MIT-licensed**, which permits embedding and redistribution through Costura as long as the copyright and license notices are retained. `FanControl.Plugins` is **proprietary** and is intentionally **not** bundled; the FanControl host supplies it at runtime, so its no-redistribution terms are respected.
