<p align="center">
  <img src="docs/banner.svg" alt="MateEngine Linux — a desktop companion for ShojiWM" width="880">
</p>

<p align="center">
  <a href="README.md"><strong>English</strong></a> ·
  <a href="README.ru.md">Русский</a> ·
  <a href="README.zh-CN.md">简体中文</a>
</p>

# MateEngine Linux

<table><tr><td><strong>A desktop companion that knows your desktop.</strong><br>
An unofficial Linux continuation of MateEngine, built from source and adapted
for ShojiWM: real window geometry, cursor reactions, dragging between monitors
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
