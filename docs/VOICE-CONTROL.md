# Local voice-control bridge

Built and installed locally on 2026-10-26 after explicit user approval.
It requires a new X3.4 application assembly **and** the matching Shoji bridge;
replacing the bridge DLL alone is insufficient. The original build24 is saved
at `port-3.4/backups/build24-before-voice-20261026`.

The published runtime sources now include the local queue/anchoring corrections:
empty filtered queues stay empty, rebuilt lists retain the right favorite/track
identity, the hide animation keeps one hand anchor, and pinned-window corrections
do not act as user drag impulses for spring bones. Private probe assets and
machine-specific shader-builder additions are not part of this source overlay.

## Transport and execution

Each process listens on `/run/user/UID/mateengine-voice-PID.sock`, mode 0600.
There is no TCP listener or remote API. Requests are one UTF-8 JSON object per
connection, followed by a newline, with an 8192-byte limit:

```json
{"version":1,"id":"unique-request-id","command":"seat.dock","monitor":"DP-1","fraction":0.5,"deadlineUtc":1793020000}
```

`deadlineUtc` is a Unix timestamp, set by the client five seconds ahead. The
server rejects expired/non-finite deadlines and deadlines over ten seconds
ahead. A client disconnect or expired request is checked before dispatch;
accepted actions are not rolled back after their execution begins. There are
no automatic retries. Only one request is dispatched at a time, on Unity's
main thread through the existing `WindowManager.Update` hook. Socket I/O runs
on a background thread. No additional managed patcher hook is required.

Responses echo `version`, `id`, `pid`, `instance`, and `ok`; failures include
`error`. `accepted: true` acknowledges the native action, **not** the completion
of a dance-loading coroutine, camera animation or compositor move. The Python
client checks socket ownership/mode and response identity before reporting it.
The voice bot journals this as `pc_command_acknowledged`.

## Commands

| Command | Fields | Behavior |
| --- | --- | --- |
| `status` | — | Process/instance, portrait state and current monitor |
| `dances.list` | — | Loaded dance names |
| `dance.play` | optional `name` | Current/first dance, or one exact name/stable ID |
| `dance.next`, `dance.previous` | — | Native filtered dance queue |
| `dance.stop` | — | Native stop handler, including sync cancellation |
| `portrait.set` | `enabled` boolean | Idempotent portrait/Big Screen request |
| `seat.dock` | optional `monitor`, `fraction` in 0–1 | Native seat on the bottom dock |
| `seat.window` | `windowId` or exact `appId`, optional `monitor`, `fraction` | Seat on one visible eligible window |
| `move` | `x`, `y` in 0–1, optional `monitor` | Position within the output's usable area |
| `hide.edge` | optional `monitor` | Native side-hide pose at the nearest allowed external edge |
| `quit` | — | Deferred `Application.Quit`, retaining normal application shutdown |

Missing `monitor` means the pet's current monitor. Window IDs include the
`window:` prefix from ShojiWM surfaces. `fraction` is a horizontal seat location
along the surface; the native pose chooser remains random. Disabled window
sitting, dragging, blocked avatar states, fullscreen/maximized supporting
windows, ambiguous targets and unavailable/stale compositor geometry fail.
Leave portrait mode before moving or sitting. Sitting retains the original
pose calibration, following and occlusion path; it does not synthesize dragging.

Dance names match case-insensitively after punctuation/space normalization,
or by stable ID. There is no fuzzy matching or automatic translation from a
Russian phonetic spelling to an English dance title. Duplicate names fail.
Native dance synchronization lead time and loading/transition delays remain.

### Installed dance bundle switch fix (2026-10-26)

The live player log reports that `dance.bundle` cannot load while another bundle
with the same files remains loaded. `SmoothPlayFlow` previously loaded the next
package before unloading the previous one. Both source trees now detach the
previous audio/animation references and unload its bundle before loading the
next package. A failed bundle load also leaves the waiting state and logs its
path. An assertion checks the single-owner invariant at the load boundary.
The shared path covers voice, UI, and automatic dance changes.

Built and installed on explicit user request, with the matching bridge DLL.
The compatibility check preserved 402 existing application types, all existing
serialized field layouts/order, and 47 bridge hooks. The restarted player
(PID 3011897) answered `status`; dance switching itself has not been exercised.
Build and installation records are in `port-3.4/builds/voice-dance-hide-managed-20261026/`;
previous DLLs and settings are in `port-3.4/backups/before-voice-dance-hide-20261026/`.

### Installed native edge-hide action (2026-10-26)

`hide.edge` calls `AvatarHideHandler.HideFromBridge` and the existing `SnapTo`
routine. It exits seating, uses the selected monitor's current geometry, excludes
shared monitor boundaries, and chooses the nearest permitted side. The original
hand anchor, hide animation, smoothing, and manual drag-to-release remain in use.
No coordinates from the user's manual placement are hard-coded. The first
installed version anchored the window but left the avatar in Idle: the native
graph enters Hide Left/Hide Right only from Drag. The user reported the failed
pose, and read-only status showed `edgeAnchored=true` with clip `PET_IDLE_3`.
The correction explicitly enters and evaluates the hide state before measuring
the hand anchor, and rejects the request if that state is missing or not entered.
Manual dragging still calls the unchanged `SnapTo` path.

The corrected application and bridge DLLs were rebuilt and installed, and
PID 3016085 answered `status`. Responses distinguish `edgeState` (actual hide
animation) from `edgeAnchored` (edge lock). Build, backup, installation, and audit
records are in `port-3.4/builds/voice-hide-pose-managed-20261026/`. The old player
hung during shutdown after saving settings and required targeted termination;
the selected model/profile remained unchanged. The correction's visual result
has not been tested. Existing `move` already releases seating and edge anchoring,
so the voice aliases for returning the pet to the screen need no native rebuild.

**Multi-instance limit:** sockets, dance actions and graceful quit identify each
process separately. Existing ShojiWM geometry selects the first pet window.
Visual actions therefore reject a layout with multiple pet surfaces; full
per-window compositor routing is not implemented by this change.

## Client

`Linux/tools/mate_control.py` is a standard-library client. The default target
must be the only process using the exact selected player executable; use
`--instance N` for a specific `--instance` index. No arbitrary PID killing or
executable-name-wide `pkill` is used. `--all-instances` is allowed only for quit.
`launch` uses a separate user systemd unit and does not duplicate an existing
process. A launch acknowledgement does not assert that the window has appeared.

Read-only example, after a rebuilt player is running:

```sh
python3 Linux/tools/mate_control.py --player-dir /path/to/player --command dances.list
```

For structured actions omit `--command` and pass one JSON object on stdin.
The client supplies protocol fields, deadlines and default x/y/fraction of 0.5.
`--config-home` scopes the selected profile to a launch; it does not edit it.
The optional voice adapter lives in `yandex-station/voice/mate_commands.py`.
Its local `mateengine.json` names the exact current installation and client.

## Installation and verification boundary

Prepare a separate build output via the existing `MATEENGINE_BUILD_OUTPUT`
environment variable and `Shoji34Build.BuildAll`, then run the matching
`Linux/tools/finalize_build.sh` on that new output. Keep the previous player intact until
the new build is accepted. The finalizer compiles all `bridge-src/*.cs`, including
`ShojiVoice.cs`, against the new application API and preserves the existing
47-hook guard. Do not run the inherited root 3.2 build script for this update.

The matching old Unity editor/cache is no longer installed. For this code-only
update, `port-3.4/tools/build_voice_managed.py` rebuilt the 219 runtime source files
against the existing Unity 6000.2 player assemblies, using the installed Roslyn
compiler without changing the engine/assets. It compiled the new bridge and
applied the existing patcher. Artifact checks preserved all 402 non-generated
application types and 2206 serialized fields, verified the four new public API
methods, and confirmed all 47 hooks. Assembly name/version also match build24.
The three unrelated KisaProbe runtime scripts absent from build24 were excluded.

The resulting player is installed at the existing `port-3.4/builds/linux-3.4`
path and started with the unchanged separate profile. Local `status` returned
`ok: true` for instance 0 on DP-1; `dances.list` returned 19 entries. The voice
service was restarted with the new handlers. These are readiness observations,
not visual confirmation of movement or playback. No dance playback, portrait
toggle, seating, application quit or visual checks were exercised.

Build records and DLL hashes are in `port-3.4/builds/voice-managed-20261026/`.
Python tests cover target ambiguity, duplicate launches, socket privacy and
acknowledgement identity; they are prepared in `Linux/tests/test_mate_control.py`
and remain unrun.
