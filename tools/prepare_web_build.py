#!/usr/bin/env python3
"""Package Unity's WebGL export for download-first startup and static hosting."""
import gzip
import hashlib
import json
from pathlib import Path
import sys


def prepare(output):
    output = Path(output)
    streaming = output / 'StreamingAssets'
    if streaming.exists() and any(p.is_file() for p in streaming.rglob('*')):
        raise RuntimeError('StreamingAssets require a local cache bridge before publishing. This build must have all assets embedded in the Unity data file.')
    build = output / 'Build'
    roles = [('loader', '*.loader.js', 'text/javascript'),
             ('framework', '*.framework.js', 'text/javascript'),
             ('code', '*.wasm', 'application/wasm'),
             ('data', '*.data', 'application/octet-stream')]
    files = []
    for role, pattern, mime in roles:
        matches = list(build.glob(pattern))
        if len(matches) != 1:
            raise RuntimeError(f'Expected one {role} file, found {matches}')
        source = matches[0]
        packed = gzip.compress(source.read_bytes(), compresslevel=9, mtime=0)
        digest = hashlib.sha256(packed).hexdigest()
        # The .bin suffix avoids the host decoding gzip before verification.
        target = build / f'{role}-{digest[:16]}.gz.bin'
        if len(packed) > 24 * 1024 * 1024:
            raise RuntimeError(f'{role} exceeds the static hosting per-file limit')
        target.write_bytes(packed)
        files.append(dict(role=role, path=target.relative_to(output).as_posix(),
                          bytes=len(packed), sha256=digest, encoding='gzip', type=mime))
    version = hashlib.sha256(json.dumps(files, sort_keys=True).encode()).hexdigest()[:16]
    (output / 'asset-manifest.json').write_text(json.dumps(dict(version=version, files=files), indent=2) + '\n')
    # Publish only the manifest-addressed payloads, never uncompressed duplicates.
    keep = {f['path'] for f in files}
    for path in build.iterdir():
        if path.is_file() and path.relative_to(output).as_posix() not in keep:
            path.unlink()
    print(f'Packaged {len(files)} verified assets, {sum(f["bytes"] for f in files) / 1048576:.1f} MB, version {version}')


if __name__ == '__main__':
    prepare(sys.argv[1])
