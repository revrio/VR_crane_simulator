#!/usr/bin/env python3
"""Restore public upstream LFS assets, verifying the exact SHA-256 in each pointer.

Useful when a ZIP/checkout was made without Git LFS. Never overwrites non-pointer files.
"""
import hashlib
from pathlib import Path
import re
import subprocess
import urllib.parse
import urllib.request

root = Path(__file__).resolve().parents[1]
revision = None
base = None
restored = 0
for asset in sorted((root / 'Assets').rglob('*')):
    if not asset.is_file() or asset.stat().st_size > 1024:
        continue
    pointer = asset.read_bytes()
    if not pointer.startswith(b'version https://git-lfs.github.com/spec/v1\n'):
        continue
    if revision is None:
        revision = subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=root, text=True).strip()
        base = 'https://media.githubusercontent.com/media/aliyunuscan/vr-crane-simulator/' + revision + '/'
    digest = re.search(rb'oid sha256:([a-f0-9]{64})', pointer).group(1).decode()
    expected_size = int(re.search(rb'size ([0-9]+)', pointer).group(1))
    relative = asset.relative_to(root).as_posix()
    request = urllib.request.Request(base + urllib.parse.quote(relative), headers={'User-Agent': 'VR-research-asset-repair'})
    with urllib.request.urlopen(request, timeout=60) as response:
        content = response.read(expected_size + 1)
    if len(content) != expected_size or hashlib.sha256(content).hexdigest() != digest:
        raise RuntimeError('Upstream data does not match LFS pointer: ' + relative)
    temporary = asset.with_name(asset.name + '.download')
    temporary.write_bytes(content)
    temporary.replace(asset)
    restored += 1
    print('Restored', relative, '(' + str(expected_size) + ' bytes)', flush=True)
print('Verified and restored', restored, 'assets.')
