# Source publication status — 2026-10-04

The current local Linux X3.4 player was built from source with Unity
6000.2.6f2 and finalized with 47 bridge hooks. The user verified transparency,
menu clicks, movement and cursor reactions during the integration work. This
source update also contains the locally checked bundled-model shader/queue and
spring-chain corrections and left/right outer-edge hiding fixes.

This publication performs file selection, provenance and privacy inspection.
It does not rebuild, reinstall or launch the application. The newly documented
clean preparation recipe has not been validated with a fresh build. No public
installer or release binary is included in this stage.

The head/spine/eye and Big Screen touch hooks use the compositor snapshot; the
builder attaches the previously absent hide component to the model/template.
These changes have now been built and run locally. Material inspection confirmed
that the tested blush overlay uses the replacement shader's default queue rather
than an opaque fallback override. Runtime logs confirmed spring initialization
after final placement, and the user confirmed the corrected local behavior,
including the outer right-edge fix delivered as a bridge-only update. This is
local verification with the user's models, not a guarantee for every bundle or
an independently reproduced fresh-clone build.

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
| Bundled-model shader replacement and author render-queue preservation | `LinuxModelShaders.cs`, `VRMLoader.cs`, editor builder |
| VRM0 spring ownership/initialization and edge-hide motion forces | `VRMLoader.cs`, `AvatarGravityController.cs`, `AvatarHideHandler.cs` |
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
- Bundle shader restoration covers the player library's exact supported shader
  names. Other/custom shaders may still be unsupported; no blanket Windows
  bundle compatibility is claimed.
