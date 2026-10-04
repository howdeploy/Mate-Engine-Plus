# Build the Linux X3.4 source overlay

This is a source publication, not a packaged installation. The running Linux
player was built from a locally reconstructed Unity project. This repository
publishes its application source, bridge, shader source and repair tools, but
does not publish the Steam player, scenes, built-in models, music, DLC,
Workshop downloads, language bundles or a user's settings.

The clean preparation recipe below is newly documented and has not been built
from a fresh clone. Manual dependency preparation is required. It does not
install or start a player. The historical root Unity project is Linux 3.2;
do not mix its scene/assets with the `Linux/` X3.4 overlay.

## Recorded inputs

| Input | Revision |
|:--|:--|
| Original Linux port | `791a8bdfedc3883d2231b065a9a4f2f9844275bd` |
| Public Windows reference | `2c5ea6b8f4cf5e1773a0816b46d9267cda5174d4` |
| Locally owned Steam player | X3.4, build `24920588`, manifest `5350674690219323409` |
| Windows Assembly-CSharp SHA256 | `ad40953040c42a635c72fc41c1a0c12d5624a48a778fb0bbb953e3c8373b40ce` |
| Editor | Unity `6000.2.6f2` (`4a4dcaec6541`), Linux build support |
| Exporter | AssetRipper `1.3.14` |
| Python readers | `Linux/tools/requirements.txt` |
| Steamworks.NET | `2024.8.0`, with matching Linux native Steam API |
| Addressables / Burst / Collections | `2.7.2` / `1.8.24` / `2.5.7` |
| Post-processing shader sources | `3.5.1` |
| LlamaLib native backend ABI | `v1.2.5` |

The public Windows reference is not assumed to contain the complete Steam X3.4
release. A purchase supplies local input; this repository does not download or
unlock paid content on someone else's behalf.

## 1. Supply local inputs

Use a fresh checkout and an isolated work directory. Inside `Linux/`, prepare:

```text
steam-source/                 your own Windows player, MateEngineX_Data/ included
recovered/ExportedProject/    unmodified AssetRipper 1.3.14 Unity project export
linux-upstream/              full source checkout of the pinned Linux port
tools/postprocessing-3.5.1/package/  matching public Unity package sources
inspection/                  generated diagnostics (never commit)
project/                     generated editable project (never commit)
builds/                      generated player and bridge artifacts (never commit)
```

Export your own installed player with AssetRipper. Keep both the source player
and unmodified export. A different player/exporter may generate different IDs;
the preparation tool rejects unknown window component identities.

```bash
python3 Linux/tools/prepare_project.py
python3 -m venv Linux/tools/reader-venv
Linux/tools/reader-venv/bin/pip install -r Linux/tools/requirements.txt
```

## 2. Prepare dependencies before the first successful import

The `overlay/` tree supplies source and source metadata, never plugin binaries.
Retain the export's VRM, Localization, TextMeshPro, UI, Discord and other managed
assemblies. The preparation script archives the extracted Addressables, Burst,
Unsafe and Steamworks.NET assemblies that the source packages replace.

Supply these dependencies into the local `project/` only:

- `Assets/ShojiLinux/Plugins/`: GtkSharp, GdkSharp, GLibSharp, GioSharp,
  AtkSharp, CairoSharp and PangoSharp `3.24.24.95`; NativeLibraryLoader;
  Tmds.DBus; Microsoft.Bcl.AsyncInterfaces; Microsoft.DotNet.PlatformAbstractions;
  Microsoft.Extensions.DependencyModel. Use compatible netstandard assemblies
  from their public packages and keep their notices.
- `Assets/ShojiLinux/Native/`: `libStandaloneFileBrowser.so`, `libsteam_api.so`,
  `libusearch_c.so` and the fixed `NativeNamedPipe.so`. Build the file-browser
  plugin from the inherited `Plugins/Linux/StandaloneFileBrowser/` source.
  Use Steamworks.NET's matching Linux native plugin; use the native USearch
  library matching the exported Cloud.Unum.USearch managed assembly.
- For `NativeNamedPipe.so`, use the reviewed original plugin in the pinned Linux
  port and `Linux/patcher/PatchNativePipe.py ORIGINAL OUTPUT`. It rejects any
  binary except SHA256 `971b8e553b8d069318f7b18383f2b610ffcee216b8522abb4b6b38de09cc81e0`;
  expected repaired hash is `39fe406f25c17461e1d07dc3b4aa98dd7a4bd0b4e3c354022bceab398f4bac52`.
- Native plugins must be enabled for Linux x86_64, with Windows/Editor platform
  selections reviewed. No native library is downloaded by the publication tools.
- Embed public Addressables `2.7.2` source under
  `project/Packages/com.unity.addressables/`, then apply the file from
  `Linux/package-overlay/`. It adds the Localization friend assembly required
  by the exported Localization DLL; keep the package's Companion License.
- Replace Windows LlamaLib backends with the public Linux `v1.2.5` equivalents
  before building if using local AI. Model weights are separate user inputs.

Runtime libraries: X11/Xext/Xrender/Xdamage/Xrandr/Xcursor/Xcomposite, GTK3,
GLib, Ayatana AppIndicator, PulseAudio or PipeWire's PulseAudio service.
`glxinfo` is required by the launch wrapper; `grim` supplies ambient desktop
sampling on compatible Wayland sessions. Build tools include Mono `mcs`,
`monodis`, Mono.Cecil `0.11.1`, OpenSSL with legacy MD4 and .NET runtime required
by TypeTreeGeneratorAPI. Set `MONO_CECIL` if its DLL is installed elsewhere.

## 3. Restore data from your original player

From the repository root, with the reader environment activated:

```bash
Linux/tools/reader-venv/bin/python Linux/tools/read_serialized_data.py TutorialMenu
Linux/tools/reader-venv/bin/python Linux/tools/restore_localization.py
Linux/tools/reader-venv/bin/python Linux/tools/restore_tutorial.py
Linux/tools/reader-venv/bin/python Linux/tools/fix_localization_asset_refs.py
Linux/tools/reader-venv/bin/python Linux/tools/restore_shaders.py
Linux/tools/reader-venv/bin/python Linux/tools/prepare_bundle_manifest.py
```

Shader restoration retains GUIDs, supplies includes and converts extracted D3D
compute assets to imported compute source. The public overlay includes the
current shader fixes. Unpublished `Hidden/LilBugShader/Refraction` is replaced
with the nearest available lilToon Refraction source; visual identity is not
claimed. Restoration scripts expect exactly the recorded input layout and fail
on ambiguous references. Do not ignore their errors.

The builder repairs Humanoid tables for the built-in scene/catalog avatars,
creates a dynamic Cyrillic font fallback from the local source font, restores
transparent clears/UI raycasts and the calibrated ledge-acquisition settings,
and rebuilds Linux language bundles with the original Addressables keys.
It restores the left hide clip's original humanoid mirror flag and builds a
strongly referenced Linux shader library for bundled models. The other X3.4
animation/controller data is retained from your export.

## 4. Build and finalize

Activate Unity normally. Enable the exported main scene in Build Settings.
Run the Editor installed on your machine:

```bash
"$UNITY_EDITOR" -batchmode -nographics -quit \
  -projectPath "$PWD/Linux/project" \
  -executeMethod Shoji34Build.BuildAll \
  -logFile "$PWD/Linux/inspection/unity-build.log"
bash Linux/tools/finalize_build.sh
```

The default output is `Linux/builds/linux-3.4/`. Finalization compiles the bridge
against that player's actual assemblies, inserts and counts **47 call sites**,
retains the original assembly and patch log in a separate build artifact
directory, and prepares `launch.sh`. It refuses an already patched build.
The matching working project has been built and finalized locally; the
fresh-clone preparation recipe remains unverified.

## Desktop integration and launch

The full reference TS/Quickshell integration is published in
[KISA Stack dotfiles](https://github.com/howdeploy/kisa-stack/tree/main/dotfiles).
Its `sources.json` pins the supporting
[ShojiWM main](https://github.com/howdeploy/ShojiWM/tree/main).
The generic input-region fix is separately proposed
[upstream](https://github.com/bea4dev/ShojiWM/pull/122). Review/install desktop
configuration separately; building the app never modifies a live WM config.

The wrapper uses X11 through Xwayland, an ARGB visual and the Shoji bridge.
`MATEENGINE_SHOJI_BRIDGE=0` disables the bridge hooks.
`__NV_DISABLE_EXPLICIT_SYNC` defaults to `1` in the reference wrapper as a local
NVIDIA workaround. Its effect on this Vulkan/X11 path is not established; it
is not evidence that a synchronization bug is fixed. Override it for your own
environment. The new build includes Vulkan and OpenGL Core renderers.

Steam must run for ownership/DLC/Workshop features. Ownership checks are
preserved. Windows-targeted Workshop bundles are not universally portable;
locally tested music/VRM content loaded, which does not establish compatibility
with every mod. Keep your profile and Workshop/DLC downloads out of git.
