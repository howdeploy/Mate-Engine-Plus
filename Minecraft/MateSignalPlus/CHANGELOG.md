# Changelog

## 1.4.1+mc26.3 — Structure coverage — 2026-10-05

- Expand Structures from 8 to 24 individually configurable clue categories:
  abandoned camps, villages, mineshafts, desert pyramids, jungle temples,
  woodland mansions, shipwrecks, monster rooms, igloos, swamp huts, pillager
  outposts, ruined portals, ocean ruins, trail ruins, Nether fossils and
  possible exposed treasure caches.
- Add Russian/English names and exact observation hints for every new switch.
  Keep crosshair-only observation, six-block reach, 750 ms focus, short-lived
  nearby evidence and existing 90-second / 10-minute cooldowns. No hidden
  block search, structure lookup or coordinate transmission is added.
- Use complete combinations for specific ruins; retain general archaeology
  comments for isolated suspicious blocks. Reuse observed shipwreck clues for
  both structure reactions and the existing opened-chest context.
- Add 48 matching Russian/English phrase pairs to the companion source.
  Companion build 20 uses its generic structure fallback for new hint IDs;
  specific lines require a separately updated companion.
- Keep the biome cooldown, selected mobs, settings, protocol, authorship and
  license unchanged. Prepare a complete source archive with the binary.

Built locally with the pinned Java 25 toolchain and offline `jar sourcesJar`.
Inspected packaged version/author metadata, preserved credits/license, detector
class and Russian/English labels and hints for all 24 categories. No gameplay/UI
tests, companion rebuild, installation or restart were performed for this patch.

## 1.4.0+mc26.3 — Situational expansion — 2026-10-05

- Correct the product names to Mate Signal Plus and Mate Engine Plus in the
  menu, metadata, messages and documentation. Use `mate-signal-plus` for future
  JAR filenames; retain existing release files and all compatibility identifiers.
- Resolve the mod menu's own labels, event names, tooltips, command feedback
  and On/Off states directly from the game's selected language dictionary.
  Retain Russian/English resources and resource-pack translation support.
- Add separately configurable durability warnings at 20% and 3% for held
  equipment and worn armour/elytra. Preserve warning memory across inventory
  moves, use repair hysteresis and include flight context for elytra.
- Add the Blocks category with 31 individual switches and comments for visible
  blocks under the crosshair. Debounce focus and limit repeated comments.
- Detect probable XP grinding from sustained, mostly identical attributed
  kills in a small area without health loss. Suppress proximity/kill chatter
  about that mob; add farming comments at most once per three minutes, with
  a separate toggle. Retain other danger warnings; add no automation.
- Limit ordinary kill reactions to once per 30 seconds.
- Add distinct crop-harvesting and wood-gathering comments based on successful
  client block-break observations, with separate switches and a shared rare
  activity cadence. Do not infer unattended farms without observed actions.
- Add the Structures category with eight kinds of near-field visible clues,
  including archaeology. Only use crosshair hits within six blocks; never
  locate structures or inspect hidden blocks. Use qualified dialogue for
  inferred structures and avoid duplicate structure/block announcements.
- Add comments for already-open normal, trapped and copper chests, single or
  double. Wait for the server's full contents packet, inspect only chest slots,
  and rate-limit to 90 seconds globally / 10 minutes per chest.
- Add optional messy-chest jokes based on occupancy, item diversity and
  mergeable matching stacks. Distinguish empty, valuable, full and uniform
  stockpiles. Never sort or move items automatically.
- Use session-observed placement and nearby already-seen clues for qualified
  chest context; do not claim reliable loot origin or ownership. Add 44 complete
  Russian/English chest dialogue pairs in the companion.
- Add compatible optional context fields and six new event types.
  Keep the original event IDs, config path, settings, licensing and credits.
- Pair with three or four Russian/English companion phrases per new dialogue pool.

Built locally with the pinned Java 25 toolchain and offline `jar sourcesJar`.
Inspected packaged metadata, RU/EN dictionaries, mixins and new event classes.
Installed into the Lunar Fabric 26.3 profile, retaining the previous JAR in
`mod-backups/MateSignalPlus-before-1.4.0-20261005/` and preserving the existing
configuration. Regular/source JARs and hashes are in `releases/MateSignalPlus-1.4.0/`.
No Minecraft launch or gameplay/UI tests; the user will verify the update.

## 1.3.1+mc26.3 — 2026-10-04

- Announce rain once per continuous rainy weather period. Moving under cover,
  through dry/snowy biomes and back into rain no longer re-arms the event.
- Keep the local rain check for the first announcement, so rain is not reported
  while the player remains underground or in a biome without rain.
- Clarify the rain toggle's Russian and English tooltips.
- Replace raw biome registry IDs in hover tooltips with translated descriptions.

Built with the pinned offline Gradle/JDK 25 toolchain; inspected packaged
version and RU/EN resources. Runtime verification remains pending.

## 1.3.0+mc26.3 — 2026-10-04

- Follow the game language with Russian/English UI and command feedback.
- Group settings into Locations, Mobs and Events; add search and enabled-state
  filters, individual biome switches and additional peaceful encounters.
- Preserve existing settings; default new mob switches to off.
- Extend the compatible UDP payload with language, biome ID, dimension,
  movement/view and sulfur-cube/undead-mount state.
- Sample terrain below airborne players and debounce biome changes; retain
  the latest stable candidate during the cooldown.
- Keep waiting nearby mobs eligible; limit daylight transitions to the
  Overworld and avoid kill praise for sulfur cubes and unridden undead mounts.
- Pair with Mate Engine Plus's rewritten RU/EN dialogue and leftward bubble
  clearance. See README for protocol and runtime-verification limitations.

Built with the pinned JDK 25 / Fabric toolchain (`jar sourcesJar --offline`).
Inspected packaged metadata, client classes, Russian/English resources,
mixins, license and credits. No gameplay/UI tests, installation or restart.
The companion changes still require a separate Mate Engine Plus build.

## 1.2.1+mc26.3 — 2026-10-04

- Introduce the Plus fork branding and identify howdeploy as its author.
- Retain the original author's credit, upstream license and provenance.
- Link the original MateSignal mod and the Mate Engine Plus project.
- Update settings, command feedback and artifact naming to the new branding.
- Preserve the `matesignal` mod ID, commands, configuration and UDP protocol.

Built with the pinned JDK 25 / Fabric toolchain; inspected packaged metadata,
license and credits. In-game verification of this version remains pending.

## 1.2.0+mc26.3 — 2026-10-04

- Port to Minecraft 26.3, Java 25 and unobfuscated Fabric Loom.
- Retain all 13 event types of the original published Fabric 1.1.0 release.
- Preserve the localhost UDP protocol used by MateEngine Linux X3.4.
- Send `drowning`, which the existing MateEngine receiver recognizes.
- Observe crafting result removal and completed food consumption directly.
- Honor the kill-message toggle; an empty mob list now disables proximity alerts.
- Apply crafting reaction probability only in MateEngine, not twice.
- Reset session state on player/world replacement and ignore large clock jumps.
- Localize mob/biome names using Minecraft's active language.
- Preserve unsaved settings across resizing; save settings atomically.
- Reuse the UDP socket and log transmission failures with a rate limit.
- Restore the Mod Menu settings entrypoint; add `/matesignal config` and
  `/matesignal test` for opening settings and checking the existing bridge.
- Include upstream license, attribution and source provenance.

Build and static inspection completed. Runtime verification in Lunar is pending.
