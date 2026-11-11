#!/usr/bin/env python3
"""Create the portable Unity source project, excluding machine caches and player builds."""
from pathlib import Path
import zipfile

root = Path(__file__).resolve().parents[1]
output = root / 'Builds/PortLab_Unity_Project.zip'
temporary = output.with_suffix('.tmp.zip')
paths = []
for name in ('Assets', 'Packages', 'ProjectSettings', 'docs', 'tools'):
    paths.extend(p for p in (root / name).rglob('*') if p.is_file()
                 and '__pycache__' not in p.parts and p.suffix != '.pyc')
for name in ('README.md', 'LICENSE', 'LICENSE.md', '.gitignore', '.gitattributes'):
    if (root / name).is_file():
        paths.append(root / name)
output.parent.mkdir(exist_ok=True)
with zipfile.ZipFile(temporary, 'w', zipfile.ZIP_DEFLATED, compresslevel=6) as archive:
    for path in sorted(paths):
        archive.write(path, 'PortLab_Unity_Project/' + path.relative_to(root).as_posix())
with zipfile.ZipFile(temporary) as archive:
    failed = archive.testzip()
    if failed:
        raise RuntimeError('ZIP CRC failed: ' + failed)
temporary.replace(output)
print(f'{output}: {len(paths)} files, {output.stat().st_size:,} bytes, CRC OK')
