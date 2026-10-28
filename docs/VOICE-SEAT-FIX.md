# Voice seating: animation entry correction

The first voice build accepted `seat.window` at 19:08:15 and `seat.dock` at
19:08:23 on 2026-10-26 (local journal time), both for process 2876772 on DP-1.
The user observed the pet standing while anchored, rather than sitting.

The routing-only explanation is ruled out by the journal and dispatcher:
these requests reached `SeatFromBridge`, not the generic move command.
The incorrect implementation assumption was that calling native `TrySnap`
from any animator state would perform the complete sitting transition.
The reviewed controller has `Drag -> WindowSit` guarded by `isWindowSit`,
with no corresponding `Idle -> WindowSit` transition. The first handler set
the sitting flag and calibrated/moved the window without entering that state.
This is the divergence point between mouse seating and voice seating.

The correction validates `WindowSit` on the active animator before acting,
chooses the native random pose, enters that state explicitly and evaluates
it before the existing calibration/pinning path. It does not synthesize a
mouse drag. Success now requires both the selected anchor and the destination
animation state. Ordinary mouse seating retains its existing state path.

`status` and seating acknowledgements now include `anchored`, `seatFlag`,
`seatState`, `seatPose`, and `animationClip`. These distinguish a geometry
acknowledgement from actual animator entry for future user checks.
The previously running build had no such telemetry: its exact live animation
state was not observed, and visual pose/contact still require user confirmation.

The self-audit used the relevant session chronology, the two journal entries,
the first handler diff, and controller transitions. It was performed without
subagents under the user's explicit delegation restriction.
