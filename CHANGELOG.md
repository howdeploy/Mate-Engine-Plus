# Changelog

## 2026-10-04 — Bundled model rendering, physics and edge hiding

- Restore `.me` bundle materials with strongly referenced, player-compiled
  shaders while retaining material properties, keywords and author render-queue
  overrides. Preserve the default queue sentinel instead of pinning the opaque
  fallback queue, which made blush overlays reveal the desktop.
- Include the lilToon transparent outline and two-pass variants used by bundled
  models. Isolate Linux helper pass names from shaders loaded by Windows bundles.
- Reinitialize VRM0 spring chains after final model placement and settings;
  remove overlapping roots that would simulate the same bones twice. Preserve
  authored spring parameters and suppress false window-motion forces while
  resting in the edge-hide animation.
- Restore the left hide clip's original humanoid mirror flag during the build.
  Floor fractional compositor pointer coordinates so the outer right edge
  remains inside its monitor and can acquire/retain hiding.
- Validation: local Unity player build, 47-hook finalization, bridge compilation
  and runtime material/physics logs; the user confirmed the resulting local
  behavior, including right-edge hiding. A fresh-clone build and arbitrary
  third-party model compatibility remain unverified. No model bundles, music,
  DLC, profiles, runtime logs or player binaries are published.

## 2026-10-04 — Linux X3.4 source publication

- Preserve the Linux port history; publish the current reconstructed X3.4
  application/Linux/shader source and ShojiWM integration under `Linux/`.
- Publish the bridge, managed/native patchers, editor build repairs, data and
  localization restoration tools, build instructions and dependency notices.
- Add English, Russian and Simplified Chinese documentation. Keep Steam/DLC
  ownership checks and exclude personal/extracted resources and settings.
- Point the reference desktop at the compatible `howdeploy/ShojiWM` main and
  KISA Stack dotfiles. Keep the generic compositor fix in upstream PR #122.

### Additional source fixes in this publication

- Route `AvatarMouseTracking` head, spine and eye cursor reads through the
  compositor snapshot, in addition to the existing workspace visibility gate.
- Route the Big Screen spring-bone touch position through the same coordinate
  conversion. The collision radius is unchanged: `0.065` in both the original
  Steam X3.4 scene and the reconstructed scene. No stronger force was introduced
  by the Linux touch port; its mouse-coordinate source was still unadapted.
- Attach the existing `AvatarHideHandler` to the default model and VRM component
  template during scene preparation. It was absent from all recorded X3.4
  scene/prefab/asset references, so edge hooks alone could not run it.
- Guard hide cleanup before its animator has initialized, including temporary
  VRM template instances destroyed before `Start`.
- Managed finalization now expects **47 bridge call sites**, up from the prior
  working player's 43. These additional fixes are source changes only: no new
  build, installation, player restart or runtime test was performed here.
