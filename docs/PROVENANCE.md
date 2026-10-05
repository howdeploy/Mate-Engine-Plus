# Provenance and publication boundary

## Ancestry

- Linux port by Marksonthegamer, commit
  `791a8bdfedc3883d2231b065a9a4f2f9844275bd`. Its history, original license,
  third-party notices and historical resource tree remain inherited in this fork.
- Original MateEngine by shinyflvre / Johnson Jason / Shiny and contributors.
  Public Windows source reference:
  `2c5ea6b8f4cf5e1773a0816b46d9267cda5174d4`.
- The current X3.4 application code was recovered from a locally owned Steam
  player and adapted for Linux. It is reconstructed source, not an assertion
  that public Windows main equals the entire Steam X3.4 release. The recorded
  input hash and exporter version are in [BUILD.md](BUILD.md).
- The Shoji bridge, editor repairs, data-restoration tools, shader/UI fixes and
  desktop integration were developed on the author's Linux desktop. The current
  application sources and reference integration are published together.

## Licenses

The [Mate Signal Plus client mod](../Minecraft/MateSignalPlus/) retains its
original [MateEngine Pro License v2.0](../Minecraft/MateSignalPlus/LICENSE.md).
Its [NOTICE.md](../Minecraft/MateSignalPlus/NOTICE.md) credits Shiny / Johnson
Jason and the VeridonNetzwerk Fabric adaptation reference. The mod's original
`pack.png` and Gradle wrapper are included with their existing attribution;
this component is separate from the Unity X3.4 overlay and its input assets.

The inherited root [LICENSE](../LICENSE) is MateEngine Pro License v2.0 with
its original third-party and asset notices. X3.4-derived application code and
the corresponding derivative integration retain
[MateEngine Pro License v2.1 and notices](../Linux/licenses/MateEngine-3.4.md)
from the public original. This repository does not relabel MateEngine as MIT
or as an unrestricted commercial distribution.

Steamworks.NET retains its [MIT notice](../Linux/licenses/Steamworks.NET.md).
Unity Addressables and PostProcessing retain their package license notices
under `Linux/licenses/`; the Addressables source overlay changes only the
Localization friend-assembly declaration. Shader/include source recovered
from the public Linux source and Unity packages retains embedded notices and
the inherited third-party licenses. VRM, lilToon, Poiyomi, Discord and other
components retain their respective original terms.

The generic documentation banner is newly authored SVG artwork; it contains
no character model, screenshot, Steam artwork or user's wallpaper.

## What this commit publishes

Only source code, source import metadata, package/build metadata, restoration
and patch scripts, source SHA256 inventory and documentation are newly added.
The existing upstream history is not rewritten or presented as our original
work. Historical root images/resources are inherited from upstream and are
distinct from the new source-only X3.4 overlay.

No newly extracted scenes, model prefabs, meshes, textures, animation clips,
fonts, music, DLC, Workshop bundles, Steam native libraries or player binaries
are added. No personal models, playlists, profiles, settings, tokens, logs,
screenshots or home paths are copied into the publication. Ownership checks
remain intact. Local restoration reads the installing user's own inputs.

Publishing source is separate from producing a redistributable installer.
Installer assets, dependency notices, reproducibility and licensing must be
settled before a clean release; no installer is supplied by this stage.
