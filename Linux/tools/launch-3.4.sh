#!/usr/bin/env bash
set -euo pipefail
PLAYER_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
export GDK_BACKEND=x11 SDL_VIDEODRIVER=x11
export MATEENGINE_SHOJI_BRIDGE="${MATEENGINE_SHOJI_BRIDGE:-1}"
export __NV_DISABLE_EXPLICIT_SYNC="${__NV_DISABLE_EXPLICIT_SYNC:-1}"
if [[ -z "${SDL_VIDEO_X11_VISUALID:-}" ]]; then
    visual_id=$(glxinfo 2>/dev/null | awk '/32 tc  0  32  0 r  y/ { if (!found) { print $1; found=1 } }')
    [[ -n "$visual_id" ]] || { echo 'No ARGB X11 visual found' >&2; exit 1; }
    export SDL_VIDEO_X11_VISUALID="$visual_id"
fi
cd "$PLAYER_DIR"
exec "$PLAYER_DIR/MateEngineX.x86_64" "$@"
