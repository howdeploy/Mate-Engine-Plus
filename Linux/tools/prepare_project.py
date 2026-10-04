#!/usr/bin/env python3
"""Prepare a separate, locally owned AssetRipper export for Linux migration.

This never downloads Steam content, launches Unity or changes an installed game.
The input export and original player remain untouched. Import/build is separate.
"""
from pathlib import Path
import re
import shutil

ROOT = Path(__file__).resolve().parents[1]
EXPORT = ROOT / 'recovered/ExportedProject'
PROJECT = ROOT / 'project'
if PROJECT.exists():
    raise SystemExit('Refusing to overwrite an existing Linux/project directory')
if not (EXPORT / 'Assets/MATE ENGINE - Scenes/Mate Engine Update.unity').is_file():
    raise SystemExit('Expected the X3.4 AssetRipper export in Linux/recovered/ExportedProject')
shutil.copytree(EXPORT, PROJECT)
(ROOT / 'inspection').mkdir(exist_ok=True)
shutil.copytree(ROOT / 'overlay', PROJECT, dirs_exist_ok=True)

# Source packages replace these extracted precompiled assemblies.
archive = ROOT / 'inspection/replaced-plugins'
archive.mkdir(exist_ok=True)
for name in ['System.Runtime.CompilerServices.Unsafe.dll', 'Unity.Addressables.dll',
             'Unity.ResourceManager.dll', 'Unity.Burst.dll', 'Unity.Burst.Unsafe.dll',
             'com.rlabrecque.steamworks.net.dll']:
    for suffix in ['', '.meta']:
        source = PROJECT / 'Assets/Plugins' / (name + suffix)
        if source.exists():
            shutil.move(source, archive / source.name)

# Retain component identities and all external scene references. These IDs are
# specific to the recorded X3.4 input; fail instead of guessing on another build.
scene = PROJECT / 'Assets/MATE ENGINE - Scenes/Mate Engine Update.unity'
text = scene.read_text()
replacements = {
    5420: ('ab80545a31502b7c99b67dad2f3a2a33', 926, '  transparentInputEnabled: 1\n'),
    5851: ('e018c97d7f38416498cf8a996671be61', 1783, ''),
}
for ident, (guid, gameobject, fields) in replacements.items():
    pattern = re.compile(rf'^--- !u!114 &{ident}\n.*?(?=^--- !u!|\Z)', re.M | re.S)
    matches = list(pattern.finditer(text))
    if len(matches) != 1 or 'guid: 263ba248c5339966c637f2b0694c166a' not in matches[0][0]:
        raise SystemExit(f'Unexpected Windows window component {ident}; project left for inspection')
    header = (f'--- !u!114 &{ident}\nMonoBehaviour:\n'
              '  m_ObjectHideFlags: 0\n  m_CorrespondingSourceObject: {fileID: 0}\n'
              '  m_PrefabInstance: {fileID: 0}\n  m_PrefabAsset: {fileID: 0}\n'
              f'  m_GameObject: {{fileID: {gameobject}}}\n  m_Enabled: 1\n  m_EditorHideFlags: 0\n'
              f'  m_Script: {{fileID: 11500000, guid: {guid}, type: 3}}\n'
              '  m_Name:\n  m_EditorClassIdentifier:\n' + fields)
    text = pattern.sub(lambda _: header, text, count=1)
# SteamDRM is a static class, not a scene MonoBehaviour. Its actual entitlement
# callers remain intact; remove only the invalid empty scene attachment.
pattern = re.compile(r'^--- !u!114 &4900\n.*?(?=^--- !u!|\Z)', re.M | re.S)
match = pattern.search(text)
if match is None or 'guid: cc0f1142f58ed206dacd496ae6179c11' not in match[0]:
    raise SystemExit('Unexpected static SteamDRM scene attachment')
text = pattern.sub('', text, count=1)
text = re.sub(r'^  - component: \{fileID: 4900\}\n', '', text, flags=re.M)
scene.write_text(text)
print('Separate Linux project prepared. Supply dependencies and restore data before importing/building.')
