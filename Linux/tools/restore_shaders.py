#!/usr/bin/env python3
"""Restore extracted shader bodies, retaining the 3.4 GUIDs and references."""
from pathlib import Path
import hashlib
import json
import re
import shutil

ROOT = Path(__file__).resolve().parent.parent
PROJECT = ROOT / "project"
UPSTREAM = ROOT / "linux-upstream"
POST = ROOT / "tools/postprocessing-3.5.1/package"
INCLUDE = re.compile(r'(^\s*#include(?:_with_pragmas)?\s+")([^"]+)(")', re.M)
SHADER = re.compile(r'\bShader\s+"([^"]+)"')
POST_PREFIX = "Packages/com.unity.postprocessing/"
POST_DEST = "Assets/ShojiShaderSources/PostProcessing/"
ANALOGUES = {"Hidden/LilBugShader/Refraction": "Hidden/lilToonRefraction"}


def destination(source):
    if source.is_relative_to(UPSTREAM):
        return source.relative_to(UPSTREAM).as_posix()
    return POST_DEST + source.relative_to(POST).as_posix()


def rewrite(source):
    def replace(match):
        include = match[2]
        if include.startswith(POST_PREFIX):
            include = POST_DEST + include[len(POST_PREFIX):]
        elif (source.parent / include).is_file():
            include = destination((source.parent / include).resolve())
        return match[1] + include + match[3]
    return INCLUDE.sub(replace, source.read_text(encoding="utf-8-sig"))


sources = {}
for root in (UPSTREAM / "Assets", POST):
    for path in root.rglob("*.shader"):
        match = SHADER.search(path.read_text(encoding="utf-8-sig"))
        if match:
            sources.setdefault(match[1], []).append(path)

report = ROOT / "inspection/shader-restoration.json"
previous = json.loads(report.read_text()) if report.exists() else {}
restored = previous.get("restored", [])
missing = []
for path in sorted((PROJECT / "Assets").rglob("*.shader")):
    text = path.read_text(encoding="utf-8-sig")
    if "DummyShaderTextExporter" not in text:
        continue
    match = SHADER.search(text)
    name = match[1] if match else str(path)
    source_name = ANALOGUES.get(name, name)
    matches = sources.get(source_name, [])
    if not matches:
        missing.append(name)
        continue
    if len({p.read_bytes() for p in matches}) != 1:
        raise RuntimeError(f"Ambiguous source for {name}: {matches}")
    source = matches[0]
    body = rewrite(source)
    if source_name != name:
        body = body.replace(f'Shader "{source_name}"', f'Shader "{name}"', 1)
    path.write_text(body, encoding="utf-8")
    restored.append({"shader": name, "target": str(path.relative_to(PROJECT)),
                     "source": str(source.relative_to(ROOT)),
                     "source_sha256": hashlib.sha256(source.read_bytes()).hexdigest(),
                     "analogue": source_name if source_name != name else None})

# Includes stay in their original tree; the shader bodies use full asset paths.
for root in (UPSTREAM / "Assets", POST):
    for path in root.rglob("*"):
        if path.suffix not in (".cginc", ".hlsl"):
            continue
        target = PROJECT / destination(path)
        target.parent.mkdir(parents=True, exist_ok=True)
        target.write_text(rewrite(path), encoding="utf-8")

# Compiled D3D compute variants in .asset files cannot be reused on Linux.
# Move their extracted originals outside Assets and import the real sources
# with the same GUID and fileID 7200000.
computes = {p.stem: p for p in POST.rglob("*.compute")}
compute_restored = previous.get("compute_restored", [])
for path in sorted((PROJECT / "Assets/ComputeShader").glob("*.asset")):
    name = re.search(r'^  m_Name: (.+)$', path.read_text(), re.M)[1]
    if name not in computes:
        raise RuntimeError(f"Missing compute source: {name}")
    meta = path.with_suffix(path.suffix + ".meta")
    guid = re.search(r'^guid: (.+)$', meta.read_text(), re.M)[1]
    backup = ROOT / "inspection/replaced-compute-assets" / path.name
    backup.parent.mkdir(parents=True, exist_ok=True)
    if backup.exists():
        raise RuntimeError(f"Backup already exists: {backup}")
    shutil.move(path, backup)
    shutil.move(meta, backup.with_suffix(backup.suffix + ".meta"))
    target = path.with_suffix(".compute")
    target.write_text(rewrite(computes[name]), encoding="utf-8")
    target.with_suffix(".compute.meta").write_text(
        f"fileFormatVersion: 2\nguid: {guid}\nComputeShaderImporter:\n"
        "  externalObjects: {}\n  currentAPIMask: 2228228\n  userData:\n"
        "  assetBundleName:\n  assetBundleVariant:\n")
    compute_restored.append(name)

# Imported .compute sources use type 3 pointers; the compiled .asset originals
# used type 2. Retaining that type makes Unity try to load HLSL as serialized data.
resources = PROJECT / "Assets/MonoBehaviour/PostProcessResources.asset"
body = resources.read_text()
for meta in (PROJECT / "Assets/ComputeShader").glob("*.compute.meta"):
    guid = re.search(r"^guid: (.+)$", meta.read_text(), re.M)[1]
    body = body.replace(f"guid: {guid}, type: 2", f"guid: {guid}, type: 3")
resources.write_text(body)

report.write_text(json.dumps({"restored": restored, "missing": missing,
                             "compute_restored": compute_restored}, indent=2))
print(f"Restored {len(restored)} shaders and {len(compute_restored)} computes; missing: {missing}")
