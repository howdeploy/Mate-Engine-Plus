# Source publication status — 2026-10-04

The current Linux X3.4 player was previously built from source with Unity
6000.2.6f2 and finalized with 43 bridge hooks. The user verified transparency,
menu clicks and movement during the earlier integration work. Subsequent
fixes cover localization, model Humanoid tables, acquisition, edge hiding,
tooltip bounds and UI alpha. The source was collected from that working tree.

This publication performs file selection, provenance and privacy inspection.
It does not rebuild, reinstall or launch the application. The newly documented
clean preparation recipe has not been validated with a fresh build. No public
installer or release binary is included in this stage.

The additional source fixes collected in this commit route head/spine/eye and
Big Screen touch coordinates through the same compositor snapshot and attach
the previously absent hide component to the model/template in the builder.
The patcher now expects 47 hooks. These new fixes have not been compiled or
applied to the running 43-hook player; runtime recovery is not yet confirmed.

## Implemented integration

| Area | Source |
|:--|:--|
| Consistent per-frame WM/pointer/monitor snapshot | `ShojiBridge.cs`, `ShojiIpc.cs` |
| Ordinary-button drag, release recovery, workspace/monitor transfer | bridge and KISA Stack `mateengine.ts`/window manager |
| Visible-workspace cursor reactions and occlusion | bridge and IPC visibility data |
| Window ledges and bottom dock; upper bar as boundary | `ShojiSeating.cs`, `ShojiSeatContact.cs`, `ShojiSeatMotion.cs` |
| Menus/tooltips and Big Screen bottom anchoring | bridge, menu application sources |
| Edge walking/hiding and shared monitor transfer guards | bridge, avatar application sources |
| Source-built transparent shaders and UI alpha | `overlay/Assets/Shader/`, shader restoration |
| Russian font fallback and localization reconstruction | editor builder and restoration tools |
| GTK dialogs, tray, PulseAudio, Linux desktop ambient sampling | Linux platform/application sources |
| Steam ownership, Workshop and purchased DLC | retained Steam source and Steamworks.NET |
| Current dance metadata for Quickshell | `ShojiMedia.cs`, KISA Stack music integration |
| Discord failed-connect file-descriptor leak | hash-guarded native pipe patcher |

## Remaining limits

- Pose-specific contact can still differ by model/animation; no claim of perfect
  contact for every VRM. Shadow click-through remains conservative during fast
  movement/dance or stale GPU readback.
- Global keyboard activity for alarm/screensaver still polls X11; this does not
  cover every native Wayland application.
- Current WM routing selects the first MateEngine window. Multiple simultaneous
  instances are not a supported/verified scenario.
- Full integration targets the pinned ShojiWM/KISA Stack configuration. Other
  Wayland compositors need their own APIs/adapters; the warning remains for them.
- Source reconstruction and native dependencies need manual preparation. The
  clean installer, release packaging and fresh-clone build validation are future
  work. Existing root `build.sh`/`install.sh` apply to the inherited 3.2 project.
- NVIDIA stability and extra ambient/shadow-render cost require measurement on
  each machine. The reference wrapper's environment setting is a workaround.
- The nearest public Refraction shader may look different from the original.
