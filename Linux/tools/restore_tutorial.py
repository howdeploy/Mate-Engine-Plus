"""Restore the X3.4 tutorial payload, remapping scene component IDs after import."""
import json
import re
from collections import defaultdict
from pathlib import Path
import yaml

ROOT = Path(__file__).resolve().parents[1]
SCENE = Path("Assets/MATE ENGINE - Scenes/Mate Engine Update.unity")
HEADER = re.compile(r"^--- !u!(\d+) &(-?\d+)(?: stripped)?$", re.M)

def read(path):
    text = path.read_text()
    headers = list(HEADER.finditer(text))
    result = {}
    for index, match in enumerate(headers):
        end = headers[index + 1].start() if index + 1 < len(headers) else len(text)
        body = yaml.safe_load(text[match.end():end])
        result[int(match[2])] = (int(match[1]), next(iter(body.values())), match.end(), end)
    return text, result

_, original = read(ROOT / "recovered/ExportedProject" / SCENE)
path = ROOT / "project" / SCENE
text, current = read(path)
def identity(kind, data):
    if kind == 1:
        return kind, data["m_Name"], data.get("m_Layer")
    script = data.get("m_Script", {})
    return kind, data.get("m_GameObject", {}).get("fileID"), script.get("guid"), script.get("fileID")

targets = defaultdict(list)
for ident, (kind, data, _, _) in current.items():
    targets[identity(kind, data)].append(ident)
mapping = {}
for ident, (kind, data, _, _) in original.items():
    if kind == 1:
        # Existing GameObjects retain their scene IDs; ambiguous names are common.
        if ident not in current or current[ident][0] != kind or current[ident][1]["m_Name"] != data["m_Name"]:
            raise RuntimeError(f"GameObject identity changed: {ident}")
        mapping[ident] = ident
    else:
        matches = targets[identity(kind, data)]
        if len(matches) == 1:
            mapping[ident] = matches[0]

def convert(value):
    if isinstance(value, dict):
        if set(value) == {"m_FileID", "m_PathID"}:
            ident = value["m_PathID"]
            if not ident:
                return {"fileID": 0}
            if value["m_FileID"] != 0 or ident not in mapping:
                raise RuntimeError(f"Unresolved tutorial reference: {value}")
            return {"fileID": mapping[ident]}
        return {key: convert(item) for key, item in value.items()}
    if isinstance(value, list):
        return [convert(item) for item in value]
    return value

folder = ROOT / "inspection/serialized-original"
rows = [row for row in json.loads((folder / "report.json").read_text()) if row["type"] == "TutorialMenu"]
if len(rows) != 1 or "error" in rows[0]:
    raise RuntimeError("Expected one fully decoded tutorial")
row = rows[0]
ident = mapping[row["path_id"]]
kind, header, begin, end = current[ident]
payload = json.loads((folder / row["json"]).read_text())
if not payload.get("steps"):
    raise RuntimeError("Original tutorial has no steps")
restored = dict(header)
restored.update({key: convert(value) for key, value in payload.items()
                 if key not in ("m_GameObject", "m_Enabled", "m_Script", "m_Name")})
body = yaml.safe_dump({"MonoBehaviour": restored}, allow_unicode=True, sort_keys=False, width=10000)
path.write_text(text[:begin] + "\n" + body + text[end:])
print(f"Restored tutorial: {len(payload['steps'])} steps, component {ident}")
