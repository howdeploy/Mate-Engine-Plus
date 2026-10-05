# Mate Signal Plus 1.4.1+mc26.3

An improved continuation of MateSignal by **howdeploy**, for **Minecraft Java
26.3 / Fabric** and **Mate Engine Plus**. The original MateSignal author,
**Shiny (Johnson Jason)**, and the Fabric port reference remain credited.

Mate Signal Plus sends client-observed game events over UDP to
`127.0.0.1:32145`. The companion selects predefined localized dialogue. No LLM,
external service or server-side mod is used for these reactions.

## What's new

- Expand the Structures menu from 8 to **24 individually configurable clues**.
- Add abandoned camps, villages, mineshafts, desert pyramids, jungle temples,
  woodland mansions, shipwrecks, monster rooms, igloos, swamp huts, pillager
  outposts, ruined portals, ocean ruins, trail ruins, Nether fossils and
  possible exposed treasure caches.
- Include Russian and English names and observation hints for all new options.
- React only to characteristic blocks already examined under the crosshair
  within six blocks. Combined clues expire after 20 seconds and must remain
  nearby. Keep the existing 90-second global / 10-minute per-kind cooldowns.
- Preserve existing settings, technical ID `matesignal`, commands and protocol.
  The 60-second biome interval is unchanged in this patch.

These are cautious interpretations of visible clues, not authoritative
structure detection. Player-built replicas can match; some generated variants
may not. No hidden blocks, buried chests or structure coordinates are searched.
Barrel contents remain outside the opened-chest feature.

## Companion compatibility

The matching Mate Engine Plus source adds **48 Russian/English dialogue pairs**
for these 16 new hints. The locally built companion build 20 already understands
`structure_sighting`, but uses generic structure dialogue for the new hint IDs.
Their specific lines require a separate companion update. The new companion
source has not been rebuilt or runtime-tested for this patch.

This JAR also retains the 1.4.0 situational features: equipment warnings at 20%
and 3%, block observations, optional farming comments, combat-chatter suppression
during probable XP grinding, crop/wood activity and opened-chest reactions.
They require a companion that supports the corresponding event types.

## Installation

1. Use Minecraft **26.3**, Java **25+**, Fabric Loader **0.19.2+** and Fabric API
   **0.160.7+26.3** or a compatible newer 26.3 build. Mod Menu 21 is optional.
2. Replace the previous MateSignal JAR in your Fabric profile with
   **`mate-signal-plus-1.4.1+mc26.3.jar`**. Do not install a `-sources.jar` as a mod.
3. Keep `config/matesignal.json` to retain your settings. Open the menu with
   `/matesignal config` or Mod Menu. Enable Minecraft messages in the companion.
4. `/matesignal test` sends a day-start event; successful UDP sending alone
   does not prove that the companion displayed it.

Existing mob selections are preserved. New encounter types remain disabled
until selected in the Mobs category.

## Release files and licensing

- `mate-signal-plus-1.4.1+mc26.3.jar` — installable client mod.
- `sources/mate-signal-plus-1.4.1+mc26.3-source.zip` — complete corresponding mod
  source, including build files, Gradle wrapper, license and attribution.
- `sources/mate-signal-plus-1.4.1+mc26.3-sources.jar` — additional source JAR.
- `SHA256SUMS` — checksums of the release files.

Publish the complete source ZIP alongside the binary on a freely accessible
public page, as required by the retained **MateEngine Pro License v2.0**.
Preserve the included `LICENSE.md` and `NOTICE.md`. This is an unofficial fork,
not endorsed by the original author, Mojang, Microsoft, Fabric or Lunar Client.

## Verification

Built successfully with the pinned Java 25 toolchain and offline Gradle
`jar sourcesJar`. Inspected packaged metadata, authorship, license/credits and
Russian/English menu coverage for all 24 categories. No gameplay/UI tests,
installation or restart were performed for this patch. Structure heuristics
and the new companion dialogue still require gameplay verification.
