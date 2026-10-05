# Mate Signal Plus

An improved fork of the discontinued
[MateSignal mod](https://www.curseforge.com/minecraft/mc-mods/matesignal),
maintained by **howdeploy** for **Minecraft 26.3 / Fabric** and
[Mate Engine Plus](https://github.com/howdeploy/Mate-Engine-Plus), the unofficial
Linux continuation of MateEngine. It uses the existing JSON-over-UDP
protocol at `127.0.0.1:32145`. The MateEngine application selects localized
messages and renders the speech bubbles. No AI service, account, server mod,
or external network service is used by this mod.

See [NOTICE.md](NOTICE.md) for upstream source revisions and credits, and
[LICENSE.md](LICENSE.md) for the unchanged upstream custom license.

This directory contains the complete client mod source and build configuration.
The companion receiver and dialogue live in
[the Unity source overlay](../../Linux/overlay/Assets/Scripts/Assembly-CSharp/).
Building this mod does not require Unity or any MateEngine assets.

## Improvements over the original

- Updated for Minecraft 26.3, Fabric and Java 25, retaining the original 13 event types.
- Fixed drowning event compatibility and duplicate crafting probability.
- Added `/matesignal config` and `/matesignal test`, with Mod Menu support.
- Improved event detection, session resets, settings persistence and UDP handling.
- Preserved localized mob and biome names for the companion's messages.
- Five settings categories: Locations, Mobs, Events, Blocks and Structures, with searchable
  biome, mob, block and structure lists. Menu labels, event names, hints and toggle states
  explicitly use the game's selected language, with Russian and English dictionaries.
- Equipment warnings at 20% and 3% remaining durability, including flight-specific
  elytra dialogue, and reactions to selected blocks under the crosshair.
- Recognize probable XP grinding from repeated attributed kills in one area,
  suppress repetitive combat chatter about farmed mobs, and offer rare optional
  farming comments instead.
- Add near-field reactions to archaeology sites and structure clues, plus
  distinct comments for crop harvesting and gathering wood.
- React to the contents of opened single/double chests, with optional gentle
  jokes about clutter, resource stockpiles and valuables, and cautious context
  from already-observed surroundings or local placement.
- Per-biome switches and encounters with happy ghasts, copper golems and
  nautiluses, alongside all registered monster types (including sulfur cubes).
- Optional biome IDs, flight context, game language and mob state for the
  companion's new bilingual dialogue. Existing UDP fields remain available.

The mod reports game events; Mate Engine Plus selects the phrases and displays
them. Existing MateEngine builds that support the same protocol remain compatible.
The technical mod ID, `/matesignal` commands and `config/matesignal.json` path
are unchanged, so existing settings carry over after the rename.

## Install in Lunar

1. Open the mods directory of your **Fabric 26.3** profile in Lunar.
2. Add `mate-signal-plus-1.4.1+mc26.3.jar`. Remove the previous MateSignal JAR
   from that profile; both versions use the same mod ID.
3. Keep Fabric API **0.160.7+26.3 or a compatible newer 26.3 version** enabled.
   Fabric Loader 0.19.2+ and Java 25+ are required. Mod Menu 21 is optional.
4. Start Mate Engine Plus and enable its Minecraft messages setting.
5. In a world, run `/matesignal test`. This sends a day-start event, independent
   of the mod's event toggles. A reported UDP send is not proof of reception.

The observed local profile directory is
`~/.lunarclient/profiles/26-1/mods/fabric-26.3/`.

Open settings using **Mod Menu → Mate Signal Plus → Config**, or `/matesignal config`
in a world. Settings are stored in the profile's `config/matesignal.json`,
with the same names used by the original Fabric release. An empty `mobs` list
disables proximity messages. The default radius is 10 blocks (range 3–64),
with creepers and zombies selected. Save applies edits; Cancel discards them.

Locations contains the master biome switch and individual biome switches.
In a world, its list comes from that world's registry (including modded biomes);
outside a world, it lists the 67 vanilla 26.3 biome IDs, including `the_void`.
`disabledBiomes` is an exclusion list; an old config with no such field enables
all locations. Mobs contains radius, search/filter controls and individual
encounter switches. The 26.3 vanilla list contains 45 monster-category types
plus happy ghast, copper golem and nautilus. That registry category also contains
neutral/passive creatures, so it does not imply an attack warning. New entries
remain off unless selected. Events contains the other 11 event switches.
It also contains separate switches for the 20% and 3% durability warnings.
The farming-comments switch controls rare XP-farm chatter. Turning it off keeps
ordinary combat chatter about the detected farm mob suppressed.
Crop harvesting and gathering wood have separate switches. Chest contents and
messy-chest jokes also have separate switches in Events. Structures contains
a master switch and 24 individually selectable kinds of visible finds.
Blocks has a master switch and 31 individual switches for ores, diamond/emerald
blocks, ancient debris, sculk variants, budding amethyst, spawners and vaults.
New durability and block reactions default to on; existing settings are retained.
The mod reads its menu dictionary for `options.languageCode` directly, including
On/Off labels, instead of relying on another client's global UI translation context.
Reopening the screen reloads the dictionary; other languages fall back to English
unless a resource pack supplies the corresponding translation.

MateEngine can suppress bubbles when its message toggle is off, the avatar
is not in an allowed animation state, or a blocking menu/object is active.
The mod does not change any of those MateEngine settings or ownership checks.

As in upstream, use this in singleplayer or private multiplayer where these
alerts are allowed. Nearby-mob alerts may violate public server rules.

## Events

| Event | Trigger / rate limit |
| --- | --- |
| `time_day`, `time_night` | Cross the original 23500 / 12750 time thresholds in the Overworld; ignore large time jumps |
| `mob_proximity` | Selected mob enters radius; at most once per 10 seconds; waiting entrants remain eligible while nearby |
| `low_health` | Health at 6 HP or less; repeat after 60 seconds |
| `low_hunger` | Hunger crosses below 10 food points |
| `death` | Local player dies |
| `rain_start` | First exposure to rain in the current rainy weather period; re-arm only when world weather clears, not when entering shelter or another biome |
| `drowning` | Air falls to half; re-arm above 80% or after leaving water |
| `sleep_start` | Player begins sleeping |
| `crafted` | Result is taken from the 2×2 or 3×3 crafting grid; at most once per second |
| `eat` | Food consumption completes; original 30% chance and 800 ms cooldown |
| `kill_confirm` | Hostile mob death with a recent damage source attributed to the local player/projectile; 30% chance and 30-second cooldown; suppressed for the detected farm mob |
| `biome_discovery` | Enabled biome changes and remains stable for 2 seconds; at most once per minute, with a join grace period |
| `equipment_durability` | Held item or worn armour/elytra at 20% or 3% durability; separate toggles, most worn eligible item first, at least 3 seconds between warnings |
| `block_sighting` | Crosshair rests on a selected visible block for 750 ms; at least 30 seconds between comments and 5 minutes for the same kind (ordinary/deepslate ore share a cooldown) |
| `xp_farm` | Probable sustained XP grinding; replaces chatter about the farm mob; at most once per 3 minutes, with a separate toggle |
| `farm_activity` | At least 6 harvested crop blocks or 12 logs, over at least 3 seconds within 12 blocks; gaps longer than 15 seconds reset the observation; shares the 3-minute activity cooldown |
| `structure_sighting` | Visible characteristic block(s) examined under the crosshair within 6 blocks; at most once per 90 seconds and once per 10 minutes for one kind |
| `chest_contents` | Open a physical single/double chest and receive its contents from the server; at most once per 90 seconds and once per 10 minutes for the same chest |

These are 19 event types (day and night are separate). Crafting probability is
applied by MateEngine itself, so the mod does not apply a second 30% filter.
Biome discovery means a change of biome, not a permanent record of visited
biomes. Kill attribution is limited to the damage information sent to the
client; server-side deaths without that information cannot be confirmed.
Sulfur cubes and the added friendly encounters do not trigger kill praise.
Camel husks, zombie horses and zombie nautiluses require a hostile passenger
for that reaction, as observed by the client at the death notification.

Durability warnings cover both hands, helmet, chestplate/elytra, leggings and
boots, including damageable modded equipment. Items in the backpack are tracked
to preserve warning memory but do not trigger warnings until held or worn.
Repair above 25% re-arms the 20% warning; above 5% re-arms the 3% warning. This
hysteresis avoids repeated alerts from Mending near a threshold. Starting below
3% emits the critical warning directly. Indistinguishable copies of the same
item share warning memory; putting all copies outside the inventory or changing
world/session resets it. No inventory data is changed. Block reactions use the
existing crosshair hit, stop while a screen is open, and never search through
walls or load terrain. Holding the crosshair on one block does not repeat it.

Probable farming requires at least 12 attributed monster kills in the last
45 seconds, at least 15 seconds in the same encounter, at least 80% of kills
of one mob type, and staying within 8 blocks of the starting point without
losing health. The history is capped at the latest 256 kills. Damage, low health
or leaving the area clears this inference; a lack of recent kills also ends it.
Only proximity/kill comments about that farm mob are suppressed. Other mob
warnings, health, air and equipment warnings remain enabled as configured.
This is a client-observed heuristic, not certain farm detection: it can also
recognize stationary grinding outside a built farm. It needs the same kill
attribution data as ordinary kill reactions, so passive/unattributed kills
cannot establish this state. No block/entity scans, server changes, clicking
or combat automation are added for this feature; the autoclicker line is dialogue.

Crop/log observations use Fabric's client block-break callback, without another
mixin. Ordinary crops, cocoa and Nether wart must be mature; sugar cane, bamboo,
pumpkins and melons also count as harvesting. Creative breaking is ignored.
Harvest comments require a recent action (within 10 seconds); health loss clears
the observation. A tree farm is not distinguished from ordinary wood gathering,
and automated crop/iron/animal farms without player actions are not inferred.

Structure reactions never request structure locations, inspect hidden blocks,
scan a volume or send coordinates. They use the existing crosshair hit within
6 blocks, held for 750 ms. Multi-block clues must have been looked at in the
last 20 seconds and still be within 8 blocks of the player:

| Reaction | Already-visible evidence |
| --- | --- |
| Archaeology site | Suspicious sand or suspicious gravel; no claim about the exact type of ruin |
| Trial chambers | Trial spawner or vault |
| Ancient city | Reinforced deepslate |
| Stronghold portal room | End portal frame |
| Bastion remnants | Gilded blackstone in the Nether |
| Nether fortress | Nether bricks and Nether brick fence in the Nether |
| End city | Purpur block/pillar and End stone bricks in the End |
| Ocean monument | Prismarine bricks, dark prismarine and a sea lantern in the Overworld |
| Abandoned camp | Campfire, cobweb and a straw bed, chest or barrel in the Overworld |
| Village | Bell and dirt path in the Overworld |
| Mineshaft | Rail, cobweb and oak/dark oak fence in the Overworld |
| Desert pyramid | Blue and orange terracotta with sandstone, cut sandstone or chiseled sandstone in the Overworld |
| Jungle temple | Mossy cobblestone, tripwire hook and dispenser in a jungle biome |
| Woodland mansion | Dark oak planks, birch planks and red carpet in the Overworld |
| Shipwreck | Planks, wooden trapdoor and stairs/slab in an ocean/beach biome |
| Monster room | Spawner and mossy cobblestone in the Overworld |
| Igloo | Snow block, red bed and furnace in the Overworld |
| Swamp hut | Spruce planks, crafting table, cauldron and potted brown mushroom in a swamp biome |
| Pillager outpost | Dark oak log, dark oak fence, birch planks and white wall banner in the Overworld; the banner pattern is not inspected |
| Ruined portal | Obsidian, crying obsidian and netherrack in the Overworld or Nether |
| Ocean ruins | Suspicious sand with sandstone, or suspicious gravel with stone bricks/mossy cobblestone, in an ocean/beach biome |
| Trail ruins | Suspicious gravel, terracotta and bricks/mud bricks in the Overworld |
| Nether fossil | Bone block with soul sand or soul soil in the Nether |
| Possible buried treasure | Already-exposed chest with sand/gravel in an ocean/beach biome, without shipwreck clues; only a possible cache, not proof of treasure |

These are hints, not authoritative structure IDs. Player-built replicas can
produce the same clues, so dialogue avoids asserting a certain structure location.
All clues in a combination must be observed; passing near a structure is not
enough. Some structure variants may lack the selected clues or spread them too
far apart. Ruins with their full combination use their specific hint; a lone
suspicious block still uses the general archaeology reaction. No underground
chests are detected. Looking at a camp barrel is only a structure clue; barrel
contents are still outside the supported chest feature.
Archaeology uses general dig-site lines because suspicious blocks occur at
several different sites ([Mojang's brush overview](https://www.minecraft.net/pt-br/article/brush)).
Enabled structure reactions take precedence over a block comment for the same
clue, with an 8-second quiet period after an announcement. Turning off a
structure kind still permits separately enabled block comments.

Chest reactions inspect only the 27 or 54 container slots of an already-open
physical chest, after the full contents packet arrives and the menu has been
open for at least 750 ms. Normal, trapped and copper chests are supported;
barrels, shulker boxes, Ender chests, minecarts and remote server menus are not
included. The mod does not open containers, query hidden inventories or move
items. Reactions distinguish empty chests, valuables, mostly uniform stockpiles,
nearly full storage (at least 90% of slots occupied), and mixed contents.

Messy-chest jokes require a double chest with at least 36 occupied slots,
18 different item IDs, and fragmented matching stacks whose consolidation
would free at least 4 slots. Stacks with different item components are not
combined for this estimate. Full, well-stacked resource storage alone does not
count as clutter. Jokes are skipped for inferred dungeon/shipwreck/structure
loot. Unknown chests use neutral wording rather than claiming the player owns
them. Disabling jokes leaves the other chest comments enabled.

Origin is not an authoritative client-side property: the open-menu packet has
no source position or loot-table ID, and the server clears a container's loot
table reference when generating its items. The mod associates the menu with
the recent chest interaction. `player_placed` means local placement was
observed during this session at that position and the current block type still
matches; it does not establish ownership. Old player-built chests remain
unknown. Dungeon clues require a spawner and mossy cobblestone; shipwreck clues
require planks, a wooden trapdoor and stairs/slabs in an ocean/beach biome.
These are reused crosshair observations from the last 20 seconds, within
8 blocks of the opened chest. Other known structure clues give a generic
structure context. Replicas can match, so dialogue qualifies these guesses.
Only mixed-content comments use origin-specific lines; other content reactions
take precedence. Placement/cooldown memories are capped at 512 chests and reset
with the world/player session. Both halves of a double chest share a cooldown.

## Companion protocol and dialogue

Events remain UTF-8 JSON datagrams to `127.0.0.1:32145`. The additional fields
are optional, so older receivers can continue reading the original fields:

| Field | Meaning |
| --- | --- |
| `language` | Minecraft language code, e.g. `ru_ru` or `en_us` |
| `dimension` | Namespaced current dimension ID |
| `movement` | `ground`, `elytra`, `flying` (ability flight), or `mounted_flight` |
| `biome_id` | Stable registry ID on `biome_discovery`; `biome` still carries the localized display name |
| `biome_view` | `below` for terrain beneath airborne players, otherwise `inside` |
| `mob_state` | Sulfur cube: `small`, `empty`, `filled`, `primed`; undead mounts: `unridden`, `hostile_rider`; empty for other types |
| `item_id`, `item_name` | Stable registry ID and localized/custom display name on `equipment_durability` |
| `equipment_kind`, `equipment_slot` | Dialogue category (`elytra`, `sword`, `pickaxe`, `head`, `chest`, `legs`, `feet`, `equipment`) and equipped slot |
| `durability_remaining`, `durability_max`, `durability_threshold` | Remaining/max durability points and crossed threshold (`20` or `3`) |
| `block_id`, `block_name` | Stable registry ID and localized display name on `block_sighting` |
| `id` on `xp_farm` | Dominant mob type in the recent farming observation |
| `activity` on `farm_activity` | `crops` or `wood` |
| `structure_hint` on `structure_sighting` | One of the 24 clue IDs in `SignalStructures.HINTS`, corresponding to the table above; an inference, not a located structure. 1.4.1 adds `abandoned_camp`, `village`, `mineshaft`, `desert_pyramid`, `jungle_temple`, `woodland_mansion`, `shipwreck`, `dungeon`, `igloo`, `swamp_hut`, `pillager_outpost`, `ruined_portal`, `ocean_ruins`, `trail_ruins`, `nether_fossil` and `buried_treasure` |
| `chest_context` | `unknown`, `player_placed`, `dungeon`, `shipwreck` or `structure`; observations/inferences, not authoritative origin or ownership |
| `chest_reaction` | `empty`, `messy`, `full`, `valuables`, `stockpile` or `mixed` |
| `chest_slots`, `chest_occupied`, `chest_types`, `chest_mergeable_slots` | Capacity, occupied slots, distinct item IDs and estimated slots freed by merging; no item list, coordinates or NBT is sent for chest analysis |

Flight over loaded Overworld/End terrain samples the biome at the surface below
the player. Underground flight and the Nether retain the local biome; the mod
does not scan through cave ceilings or the Nether roof. No terrain is loaded
on demand. Rapid biome crossings shorter than two seconds are intentionally
quiet. During cooldown, the latest stable biome can still be announced if the
player remains there; intermediate crossed biomes are not queued.

The updated Mate Engine Plus receiver provides four complete ground/inside and
four flight phrases per vanilla biome, specific mob encounters and rewritten
event dialogue in Russian and English. It selects phrases locally without an
LLM and avoids an immediate repeat within each phrase pool. Unknown/modded
biomes use a grammatical label-style fallback. Mob states describe the moment
of proximity detection; changing a nearby cube's contents is not a separate
event. Old clients without the new fields remain supported with generic lines.
Dialogue uses the game language when supplied, otherwise the companion locale;
languages other than Russian currently fall back to English in this catalogue.

Version 1.3.0 contains the new menu and optional protocol fields. Contextual
dialogue and bubble placement also require an updated Mate Engine Plus build;
updating only the mod does not replace the companion's existing phrases.
The new 1.4.0 situational events require the matching updated companion;
older receivers ignore these new event types and retain the original reactions.
Equipment and block comments each have three Russian/English variants; farming
has four, including the requested playful lines. Harvesting/wood gathering
and archaeology also have four variants; other structure hints have three.
Chest comments provide 44 Russian/English pairs across 11 pools.
Version 1.4.1 adds 16 structure hints and 48 corresponding Russian/English
dialogue pairs in the companion source. Companion build 20 understands the
event but uses general structure lines for these new IDs. The specific phrases
require a companion rebuilt with the updated `MinecraftDialogue.Structures.cs`;
installing the mod JAR alone cannot replace Unity dialogue. The 60-second biome
cooldown and the companion's bubble replacement behavior are unchanged.

Four small client mixins observe crafting `ResultSlot.onTake`,
`LivingEntity.completeUsingItem` / `handleEntityEvent`, container contents in
`ClientPacketListener.handleContainerContent`, and `BlockItem.place`. They observe actions;
they do not cancel them or change inventory, damage, packets or game rules.

## Build

JDK 25, Gradle wrapper 9.6.0, Fabric Loom 1.17.12. Versions are pinned in
`gradle.properties` and `build.gradle`. No Yarn mappings or remap step is used
for unobfuscated Minecraft 26.3.

Run from this directory with JDK 25 installed:

```sh
JAVA_HOME=/path/to/jdk-25 ./gradlew jar sourcesJar --console=plain
```

Outputs are in `build/libs/`. The regular JAR is installable; `-sources.jar`
contains source files for inspection and is not a second mod.
The included Gradle Wrapper downloads the pinned Gradle version; dependency
downloads require network access on the first build. Build caches and output
directories are excluded from Git.

To prepare an update, change `mod_version`, record changes in `CHANGELOG.md`,
and build. Keep the complete corresponding source publicly available with the
release, as required by the retained upstream license. This repository includes
the source; it does not commit generated release JARs.

### Correspondence to the prepared 1.4.1 release

The Java sources, language resources, mixin configuration, icon, Gradle wrapper
and build files match the complete source archive prepared for
`mate-signal-plus-1.4.1+mc26.3.jar`. Documentation and attribution publication
notes were updated for this repository, and `fabric.mod.json` now points its
homepage at the renamed `Mate-Engine-Plus` repository. No gameplay code changed
during publication. These metadata/documentation updates and build timestamps
mean that a fresh build is not claimed to be byte-identical to the earlier JAR.

- Prepared JAR SHA256: `d2bbb14215ae00b2fb7b8bf01db004a617f82f77a6cd727f3dbca2eae9c3e86e`
- Complete release source ZIP SHA256: `39438f87d4d4cddd829ca2237b381778be737aa3e562c57cbed95b7d2efb8b9e`

The corrected product names are Mate Signal Plus and Mate Engine Plus. The
1.4.0 build uses the corrected `mate-signal-plus` filename prefix. Older releases
retain their original filenames. The companion repository is now
`howdeploy/Mate-Engine-Plus`; old `howdeploy/Meta-Engine-Plus` links redirect
to it. The technical `matesignal` ID, commands, settings path and protocol
are unchanged.

## Verification status

Version 1.4.1 was built locally on 2026-10-05 with the pinned Java 25 toolchain
and offline `jar sourcesJar`. Inspected the packaged version, howdeploy author
metadata, preserved upstream credits/license, detector class and complete
Russian/English labels and hints for all 24 structure categories. The companion
source contains all 24 corresponding dialogue pools. No gameplay/UI tests,
companion rebuild, installation or restart were performed for this patch.

Version 1.4.0 situational expansion was built locally on 2026-10-05 with the
pinned Java 25 toolchain and offline `jar sourcesJar` tasks. Inspected the
packaged metadata, Russian/English dictionaries, four client mixins and new
event classes. Installed the regular JAR into Lunar's Fabric 26.3 profile;
the previous JAR is backed up outside its mods directory and the existing
`~/.minecraft/config/matesignal.json` was preserved byte-for-byte. Release and
source JARs were retained in the local release directory with SHA256 sums. Minecraft was
not launched. In-game verification of menu translation, durability thresholds,
block cooldowns, farming, structure clues and chest contents is pending.

Version 1.3.1: rebuilt successfully with the same offline `jar sourcesJar`
tasks after the user's rain-spam report. Inspected packaged version and both
language resources. Biome tooltips now use localized prose instead of raw
registry IDs. In-game validation of these corrections remains pending.

Version 1.3.0: `jar sourcesJar --offline --console=plain` completed successfully
with the pinned JDK 25 / Fabric toolchain. Inspected the packaged 1.3.0 metadata,
client entrypoints/classes, RU/EN language resources, mixins, license and credits.
No tests, game launch, companion build/launch, installation or restart was
performed for this release. Menu layout, live flight sampling, all dialogue
events and model-specific bubble clearance still require runtime verification.

2026-10-04: version 1.2.0 was compiled against Fabric API 0.160.7+26.3 and
Minecraft 26.3 using JDK 25; inspected JAR metadata, entrypoints and mixin target bytecode in the
installed Minecraft JAR. No Minecraft launch, UI test, automated gameplay
test, or live UDP event was performed by the agent. The user subsequently
confirmed Minecraft/companion interaction; the full event matrix has not been
verified.

Version 1.2.1 was built with the same pinned toolchain. Packaged metadata,
license and credits were inspected. Its updated branding has not been checked
inside Minecraft; no game or client restart was performed.
