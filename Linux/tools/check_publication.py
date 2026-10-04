#!/usr/bin/env python3
"""Inspect new index blobs; never read or print local input/profile contents."""
import re
import subprocess
from pathlib import PurePosixPath

def git(*args):
    return subprocess.check_output(['git', *args])

paths = git('diff', '--cached', '--name-only', '--diff-filter=ACMR', '-z').decode().split('\0')
allowed_roots = {'README.md', 'README.ru.md', 'README.zh-CN.md', '.gitignore', 'CHANGELOG.md'}
allowed_suffixes = {'.cs', '.meta', '.shader', '.compute', '.cginc', '.hlsl', '.asmdef',
                    '.asmref', '.py', '.sh', '.md', '.txt', '.json', '.svg'}
private_dirs = {'project', 'steam-source', 'recovered', 'inspection', 'builds',
                'user-profile', 'baseline', 'UserSettings', 'DLC', 'AudioClip',
                'Texture2D', 'Mesh', 'AnimationClip', 'RebuiltAvatars', 'RebuiltFonts'}
patterns = [re.compile(r'/home/(?!\$)[a-zA-Z][a-zA-Z0-9_-]*/'),
            re.compile(r'gh[pousr]_[A-Za-z0-9]{20,}|github_pat_[A-Za-z0-9_]{20,}'),
            re.compile(r'-----BEGIN (?:RSA |OPENSSH |EC )?PRIVATE KEY-----'),
            re.compile(r'(?i)(?:sk-proj-|xox[baprs]-)[A-Za-z0-9_-]{15,}')]
findings = []
count = 0
for name in filter(None, paths):
    path = PurePosixPath(name)
    count += 1
    if name not in allowed_roots and not name.startswith(('Linux/', 'docs/')):
        findings.append((name, 'outside publication allowlist'))
    if path.suffix not in allowed_suffixes and name != '.gitignore':
        findings.append((name, 'resource/binary extension'))
    if set(path.parts) & private_dirs or any('dlc' in part.lower() for part in path.parts):
        findings.append((name, 'private/resource directory'))
    data = git('show', ':' + name)
    if b'\0' in data:
        findings.append((name, 'binary blob'))
        continue
    text = data.decode('utf-8-sig')
    if any(pattern.search(text) for pattern in patterns):
        findings.append((name, 'personal path or credential marker'))
for name, reason in findings:
    print(name + ': ' + reason)
print(f'Inspected {count} staged publication files; {len(findings)} findings.')
raise SystemExit(bool(findings))
