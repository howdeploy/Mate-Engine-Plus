# Changelog

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
