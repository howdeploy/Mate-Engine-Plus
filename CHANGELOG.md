# Changelog

## 2026-10-05 — Minecraft project page links

- Publish the accumulated Minecraft integration source and corrected Mate
  branding, including the 1.4.1 structure dialogue additions.
- Preserve the old `minecraft-integration-meta-signal-plus` README anchor for
  existing mod-page links and add a stable `minecraft-integration` anchor.
- State the current mod version and required/optional dependencies at the
  start of the integration section. Retain the companion compatibility and
  build-verification limits below.

## Unreleased — Expanded Minecraft structure clues

- Add 48 Russian/English phrase pairs for 16 additional structure hints from
  Mate Signal Plus 1.4.1: camps, villages, mineshafts, pyramids, jungle temples,
  mansions, shipwrecks, monster rooms, igloos, swamp huts, outposts, ruined
  portals, ocean/trail ruins, Nether fossils and possible exposed treasure.
- Keep all claims qualified: the client uses recently examined nearby blocks,
  not authoritative structure locations. No hidden chests or blocks are searched.
- Preserve existing event IDs, user settings, biome cadence and bubble behavior.

Companion source prepared only; no new Unity build, installation, restart or
gameplay/UI verification. Build 20 uses generic structure lines for new hints.

## Unreleased — Minecraft situational expansion

- Correct the product names to Mate Engine Plus and Mate Signal Plus across
  documentation, the banner and source comments, retaining compatibility IDs.
- Accept the compatible equipment, block, farm, structure and chest event additions
  from Mate Signal Plus 1.4.0 without changing existing event numbers or gates.
- Add three Russian/English lines per new dialogue pool for the 20% and 3%
  durability thresholds, including elytra flight and individual armour pieces,
  and for ores, sculk variants, spawners, vaults and other selected blocks.
- Document the companion mod's Blocks category and explicit game-language
  resolution for menu labels, event names, tooltips and toggle states.
- Add four Russian/English farming comments for the mod's probable-grinding
  context. The mod suppresses repetitive proximity/kill lines about the farm
  mob, limits ordinary kill chatter and lets users disable farming comments.
  No autoclicker or combat automation is implemented.
- Add separate crop/wood activity lines and eight structure-clue pools,
  including archaeology sites. React only to already-visible near-field
  evidence, use qualified wording and avoid duplicate structure/block chatter.
- Add 44 Russian/English chest dialogue pairs for contents, optional clutter
  jokes and cautious observed context. The mod waits for an opened chest's
  server-sent inventory, sends aggregate counts only and limits repeated
  comments. Do not infer certain ownership or origin from loot contents.

Built and installed locally on 2026-10-05 as build 20, using Unity 6000.2.6f2;
finalization verified all 47 ShojiWM bridge call sites. Restarted with the
existing user profile and confirmed the ShojiWM connection and UDP listener
on port 32145. Saved the previous build in `linux-3.4-before-situational-build20`
and configuration snapshots in `inspection/settings-before-build20` under the
local port workspace. Core settings, avatar list and favorites were retained.
Mate Signal Plus 1.4.0 was also built and installed into Lunar's Fabric 26.3
profile. Gameplay/UI verification remains with the user; no public binary release.

## Unreleased — Minecraft dialogue and bubble placement

- Add a shared Russian/English dialogue catalogue for both avatar templates:
  rewritten event messages, four ground/inside and four flight lines for each
  of 67 vanilla biome IDs, and specific encounters for 48 selectable mob types.
- Use stable biome IDs and flight context, avoid uninflected Russian name
  substitution, and avoid immediate repeats within each phrase pool.
- Handle sulfur-cube state and hostile riders on undead mounts; add friendly
  happy-ghast, copper-golem and nautilus encounters.
- Read the game's locale from optional packet fields; keep legacy packets and
  event aliases compatible. Unknown IDs use generic complete sentences.
- Add leftward Minecraft bubble clearance to the existing live-head tracking.
  Retain all application settings, message gates and ownership checks.
- Document the companion mod's localized Locations/Mobs/Events menu and
  optional protocol fields. Mod 1.3.1+mc26.3 also fixes repeated rain messages
  during continuous rain and localizes biome-toggle tooltips.
- Build and restart the updated Linux companion locally on 2026-10-04,
  retaining the existing user profile and backing up the previous player and
  settings. Confirm model loading, the ShojiWM connection and the Minecraft
  UDP listener from startup logs. Gameplay and visual verification remain
  pending; this build has not been published.

## 2026-10-04 — Plus branding and Minecraft integration

- Introduce the Plus branding for MateEngine Linux while retaining Git history,
  upstream attribution and the existing licenses.
- Describe the unofficial Linux port and its custom improvements under the
  new project name; update the README titles and banner.
- Document Mate Signal Plus by howdeploy, the Minecraft 26.3 / Fabric
  continuation of MateSignal, its event fixes and the local companion protocol.

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
