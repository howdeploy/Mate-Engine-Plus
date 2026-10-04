#!/usr/bin/env bash
# Patch only the isolated Linux build. No installation, reload or player launch.
set -euo pipefail
PORT_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PLAYER="${1:-$PORT_ROOT/builds/linux-3.4}"
MANAGED="$PLAYER/MateEngineX_Data/Managed"
CECIL="${MONO_CECIL:-/usr/lib/mono/gac/Mono.Cecil/0.11.1.0__0738eb9f132ed756/Mono.Cecil.dll}"
[ -f "$PLAYER/MateEngineX.x86_64" ] || { echo 'Linux player build is missing' >&2; exit 1; }
[ ! -e "$MANAGED/MateEngine.ShojiBridge.dll" ] || { echo 'Build already patched' >&2; exit 1; }
PATCH_OUT="$(mktemp -d "$PORT_ROOT/builds/bridge-player.XXXXXX")"
cp -p "$MANAGED/Assembly-CSharp.dll" "$PATCH_OUT/Assembly-CSharp.original.dll"
refs=()
for name in mscorlib netstandard System System.Core UnityEngine.CoreModule UnityEngine.SharedInternalsModule \
    UnityEngine.UIModule UnityEngine.UI UnityEngine.InputLegacyModule UnityEngine.TextRenderingModule \
    UnityEngine.PhysicsModule UnityEngine.IMGUIModule UnityEngine.AnimationModule UnityEngine.AudioModule UnityEngine.JSONSerializeModule; do
    refs+=("-r:$MANAGED/$name.dll")
done
refs+=("-r:$PATCH_OUT/Assembly-CSharp.original.dll")
mcs -nologo -target:library -optimize+ -nostdlib -noconfig \
    -out:"$PATCH_OUT/MateEngine.ShojiBridge.dll" "${refs[@]}" "$PORT_ROOT"/bridge-src/*.cs
mcs -nologo -out:"$PATCH_OUT/PatchAssembly.exe" -r:"$CECIL" "$PORT_ROOT/patcher/PatchAssembly.cs"
mono "$PATCH_OUT/PatchAssembly.exe" "$PATCH_OUT/Assembly-CSharp.original.dll" \
    "$PATCH_OUT/MateEngine.ShojiBridge.dll" "$PATCH_OUT/Assembly-CSharp.dll" "$MANAGED" | tee "$PATCH_OUT/patch.log"
expected=$(sed -n 's/^bridge call sites: //p' "$PATCH_OUT/patch.log")
MONO_PATH="$MANAGED:$PATCH_OUT" monodis "$PATCH_OUT/Assembly-CSharp.dll" > "$PATCH_OUT/Assembly-CSharp.il"
calls=$(rg -c 'MateEngine.Shoji.ShojiBridge::' "$PATCH_OUT/Assembly-CSharp.il")
[ "$expected" = 47 ] && [ "$calls" -eq "$expected" ] || { echo "Unexpected hooks: $calls/$expected" >&2; exit 1; }
# Keep Windows-only LlamaLib binaries in the build archive, not the Linux payload.
LLM_ROOT="$PLAYER/MateEngineX_Data/StreamingAssets/undreamai-v1.2.5-llamacpp"
mkdir -p "$PATCH_OUT/windows-backends"
for backend in "$LLM_ROOT"/windows-*; do
    [[ -e "$backend" ]] || continue
    mv "$backend" "$PATCH_OUT/windows-backends/"
done
cp -p "$PATCH_OUT/Assembly-CSharp.dll" "$PATCH_OUT/MateEngine.ShojiBridge.dll" "$MANAGED/"
cp -p "$PORT_ROOT/tools/launch-3.4.sh" "$PLAYER/launch.sh"
chmod +x "$PLAYER/launch.sh"
printf '3625270\n' > "$PLAYER/steam_appid.txt"
sha256sum "$PLAYER/MateEngineX.x86_64" "$MANAGED/Assembly-CSharp.dll" "$MANAGED/MateEngine.ShojiBridge.dll" > "$PATCH_OUT/SHA256SUMS"
printf 'Isolated build prepared: %s\nBridge artifacts: %s\n' "$PLAYER" "$PATCH_OUT"
