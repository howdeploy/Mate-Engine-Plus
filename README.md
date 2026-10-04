<p align="center">
  <img src="docs/banner.svg" alt="Mate Engine Plus — a desktop companion for ShojiWM" width="880">
</p>

<p align="center">
  <a href="README.md"><strong>English</strong></a> ·
  <a href="README.ru.md">Русский</a> ·
  <a href="README.zh-CN.md">简体中文</a>
</p>

# Mate Engine Plus

<table><tr><td><strong>A desktop companion that knows your desktop.</strong><br>
An unofficial Linux port of MateEngine with custom features and improvements,
maintained by <strong>howdeploy</strong>. Built from source and adapted for ShojiWM:
real window geometry, cursor reactions, dragging between monitors
and workspaces, window ledges and the bottom dock.</td></tr></table>

**Source-first publication.** The current X3.4 application source and our
integration fixes are in [Linux/](Linux/). A clean downloadable installer is a
separate future stage. This repository does not contain the author's private
models, music, DLC, Workshop downloads, profiles or extracted Steam resources.

## Stack

| Application | Desktop | Shell | Platform |
|:--|:--|:--|:--|
| Unity 6000.2.6f2 · C# | ShojiWM · TypeScript IPC | Quickshell · KISA Stack | Linux x86_64 · Xwayland |

This is a fork of [Marksonthegamer's Linux port](https://github.com/Marksonthegamer/Mate-Engine-Linux-Port)
of [shinyflvre's MateEngine](https://github.com/shinyflvre/Mate-Engine).
It is independent of both authors and is not an official Steam Linux release.
Previously published as **MateEngine Linux**, this project continues under the
**Mate Engine Plus** name with its existing history and upstream credits intact.

## What the integration does

- Moves the character with ordinary left-button dragging through compositor
  IPC, with release recovery and monitor/workspace transfer.
- Uses a consistent per-frame window/cursor snapshot for hands, head, eyes,
  reactions and menus; hidden workspaces do not react to your cursor.
- Supports sitting on application windows and the bottom dock. The upper bar
  remains a layout boundary. Seating includes pose/contact and occlusion work.
- Handles menu/tooltip bounds, Big Screen bottom anchoring and exit, walking and
  outer-edge hiding while keeping shared monitor edges available for transfer.
- Restores Linux shader sources, transparent background, UI alpha, localization
  and Cyrillic fonts; rebuilds Humanoid tables for extracted built-in models.
- Retains Steam ownership/DLC checks and Workshop loading, and publishes the
  current dance track metadata for the desktop music widget.
- Uses GTK dialogs, a native tray and PulseAudio/PipeWire audio detection;
  includes a hash-guarded repair for the Discord native pipe FD leak.

Some model-specific poses still need calibration. Native Wayland-wide keyboard
tracking and multiple simultaneous pets remain limited.
See [implemented behavior and verification limits](docs/STATUS.md).

<a name="minecraft-integration-meta-signal-plus"></a>
<a name="minecraft-integration"></a>

## Minecraft integration: Mate Signal Plus

**Mate Signal Plus**, maintained by **howdeploy**, is our improved continuation
of the discontinued [MateSignal mod](https://www.curseforge.com/minecraft/mc-mods/matesignal)
for **Minecraft 26.3 / Fabric**, including Fabric profiles in Lunar Client.
The port retains all 13 original event types: nearby hostile mobs, low health
and hunger, death, day/night transitions, rain, drowning, sleep, crafting,
eating, hostile-mob kills and biome changes.

**Current mod version: 1.4.1+mc26.3.** Requires Minecraft Java 26.3, Java 25+,
Fabric Loader 0.19.2+ and Fabric API 0.160.7+26.3 or a compatible newer 26.3
build. Mod Menu 21 is optional; `/matesignal config` also opens the settings.
Install the mod on the client only. An updated Mate Engine Plus is required
for its companion dialogue; see the compatibility details below.

Improvements include corrected drowning events, removal of the duplicate
crafting probability filter, more reliable event handling, localized mob/biome
names, persistent settings, and `/matesignal config` / `/matesignal test`
commands with Mod Menu support.

Enable Minecraft messages in Mate Engine Plus and run the mod in your Fabric
client. Events are sent locally to `127.0.0.1:32145`; the companion chooses
localized phrases and displays speech bubbles. This integration uses predefined
messages and requires no AI service or server-side mod. The technical mod ID
remains `matesignal` to preserve existing settings.

The Minecraft dialogue update adds a shared authored Russian/English catalogue
for all 13 event types, 67 vanilla biome IDs and 48 selectable mob types. Each
biome has four ground/inside phrases and four flight phrases, including the
26.2 sulfur caves and 26.3 dappled forest. Russian sentences are complete rather
than assembled around an uninflected biome name. Encounters distinguish sulfur
cube contents/size/lit TNT and hostile riders on undead mounts; new peaceful
encounters include happy ghasts, copper golems and nautiluses. Each phrase pool
avoids immediate repeats.

An updated mod supplies stable IDs, dimension, flight/view context, mob state
and its active language. The companion uses that language (Russian or English,
with English fallback), or its own locale for older clients. Original packet
fields and event aliases remain supported. Unknown biomes/mobs use label-style
fallback sentences. No new localized asset bundle is needed: the catalogue in
`MinecraftDialogue*.cs` is shared by the built-in and imported avatar templates.
Minecraft bubbles have an additional leftward head clearance, adjustable via
`AvatarMinecraftMessages.headClearance` (default 90 canvas units).

The situational expansion supports Mate Signal Plus 1.4.0.
It adds `equipment_durability`, `block_sighting`, `xp_farm`, `farm_activity`,
`structure_sighting` and `chest_contents` events. It provides three or four Russian/English variants per new
dialogue pool: equipment warnings at 20% and 3%, special elytra-in-flight warnings,
and comments on ores, sculk, spawners and other selected blocks under the crosshair.
The mod adds a Blocks category with 31 switches and reads menu/event labels
directly from the game's language dictionary. These additions were built and
installed locally on 2026-10-05 as companion build 20; the mod was also built
and installed into the Lunar Fabric 26.3 profile. Gameplay verification is pending.

The mod also suppresses repetitive combat messages about a mob during probable
XP grinding, based on sustained attributed kills in one area without health
loss. It sends optional farming comments at most once per three minutes; the
companion has four Russian/English variants. The autoclicker joke is only text,
and no gameplay automation is performed.

Crop harvesting and wood gathering have distinct comments and switches.
The Structures category provides 24 types of near-field reactions, including
archaeology sites. It uses only characteristic blocks already examined under
the crosshair within six blocks, with short-lived visual evidence for combined
patterns. It never searches structure locations or hidden blocks. Dialogue treats
ambiguous clues as inferences; a structure comment replaces a block comment about
the same clue. Player-built replicas may produce the same clues.

Mate Signal Plus 1.4.1 expands the original eight clues with abandoned camps,
villages, mineshafts, desert pyramids, jungle temples, woodland mansions,
shipwrecks, monster rooms, igloos, swamp huts, pillager outposts, ruined portals,
ocean ruins, trail ruins, Nether fossils and possible exposed treasure caches.
The companion source adds three Russian/English phrase pairs per new hint
(48 pairs total). All required nearby clues must have been examined; some
structure variants may not match. Hidden treasure is never located. Companion
build 20 falls back to generic structure lines for these new IDs; this source
update has not yet been rebuilt into the running player. The biome cooldown
and bubble replacement behavior remain unchanged.

Chest reactions add 44 Russian/English pairs for empty chests, valuables,
uniform stockpiles, nearly full storage, mixed contents and gentle clutter jokes.
The mod inspects only an opened normal/trapped/copper chest's 27 or 54 slots
after receiving its contents from the server. Jokes require a double chest with
at least 36 occupied slots, 18 item IDs and matching partial stacks whose merging
would free at least 4 slots; resource storage alone does not imply clutter.
Chest reactions and jokes have separate switches in the mod's Events category,
with cooldowns of 90 seconds overall and 10 minutes per chest. No items are moved.

The client does not receive a permanent chest-origin or ownership tag. Locally
observed placement is remembered for the session; dungeon/shipwreck context uses
only previously examined nearby blocks, and is phrased as an inference. Old
player-built chests remain unknown, and unknown chests do not get ownership
claims. Content-specific reactions take priority over origin-specific mixed
chest lines. UDP carries context and aggregate counts, not the chest's item list
or coordinates. Existing event numbers and receiver gates remain unchanged.

The mod's `/matesignal config` screen follows the game language and has
Locations, Mobs, Events, Blocks and Structures categories, individual switches and
search/filter controls. Existing configuration and selected mobs are retained.
Mate Signal Plus 1.3.1+mc26.3 also reports rain only once per continuous rainy
period and localizes the biome-toggle tooltips. The updated companion was built
and restarted locally on 2026-10-05 with the existing user profile. Unity completed
the build successfully and finalization verified all 47 ShojiWM bridge call sites.
Startup logs confirm the ShojiWM connection and Minecraft UDP listener on port
32145. The previous player and settings were backed up; `settings.json`, the
avatar list and saved favorites remained unchanged. Visual placement and gameplay
verification remain pending. This local build is not a published release.

The companion mod is maintained separately; its public download is being
prepared. This repository currently publishes the application source, not the
Minecraft mod JAR.

## Build from source

Start with [the X3.4 build guide](docs/BUILD.md). The current project was
reconstructed from a locally owned Steam X3.4 player; the guide requires your
own input resources, matching dependencies and Unity activation. The source
overlay is complete for the collected application code, but a fresh-clone build
of the newly documented preparation recipe has not yet been validated.

The inherited project at the repository root is the older Linux 3.2 base.
Its scripts and resource history are retained for provenance, not presented as
a turnkey 3.4 installation. [The original port README](docs/UPSTREAM-LINUX.md)
is archived separately.

## Desktop compatibility

Use [howdeploy/ShojiWM main](https://github.com/howdeploy/ShojiWM/tree/main)
with the revision pinned by [KISA Stack dotfiles](https://github.com/howdeploy/kisa-stack/tree/main/dotfiles).
The reference configuration supplies the app's IPC and dock ledges. Generic
input-region fallthrough is proposed separately in
[ShojiWM PR #122](https://github.com/bea4dev/ShojiWM/pull/122).

Full desktop integration targets this ShojiWM configuration. Other Wayland
compositors require their own adapters. Source publication does not install a
desktop configuration or change a running player.

## Documentation

| Start here | Source and maintenance |
|:--|:--|
| [Build guide](docs/BUILD.md) | [Source layout](Linux/README.md) |
| [Current status and limits](docs/STATUS.md) | [Provenance and licenses](docs/PROVENANCE.md) |
| [Desktop dotfiles](https://github.com/howdeploy/kisa-stack/tree/main/dotfiles) | [Changelog](CHANGELOG.md) |

## Ownership and licensing

Keep Steam running for purchased content. This fork preserves ownership checks
and does not distribute or unlock another user's paid resources. Windows
Workshop bundles are not guaranteed to work on Linux; music and VRM content
worked locally, which does not establish compatibility with every mod.

MateEngine uses its custom **MateEngine Pro License**, not MIT. The inherited
Linux base retains [its original license](LICENSE); the X3.4 application source
retains [the corresponding upstream license](Linux/licenses/MateEngine-3.4.md).
Third-party terms and asset copyright notices remain applicable.
See [provenance](docs/PROVENANCE.md) before redistributing.
