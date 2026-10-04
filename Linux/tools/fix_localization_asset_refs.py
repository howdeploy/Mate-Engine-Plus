"""Correct imported/native PPtr hints in the already restored localization files."""
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
ASSETS = ROOT / "project/Assets"
native_guids = set()
for meta in ASSETS.rglob("*.asset.meta"):
    text = meta.read_text()
    if "NativeFormatImporter:" in text:
        native_guids.add(re.search(r"^guid: (\w+)$", text, re.M)[1])
files = json.loads((ROOT / "inspection/localization-restoration.json").read_text())["files"]
pattern = re.compile(r"(guid: (\w+)\n([ \t]*)type: )3\b")
changed = {}
for name in files:
    path = ASSETS / name
    text = path.read_text()
    count = [0]
    def replace(match):
        if match[2] not in native_guids:
            return match[0]
        count[0] += 1
        return match[1] + "2"
    corrected = pattern.sub(replace, text)
    if count[0]:
        path.write_text(corrected)
        changed[name] = count[0]
print(json.dumps(changed, indent=2, ensure_ascii=False))
