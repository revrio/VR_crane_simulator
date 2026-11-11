#!/usr/bin/env bash
set -euo pipefail
portlab_root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
exec "$portlab_root/Builds/OperationalLinux/PortLabOperations.x86_64" -force-vulkan -screen-fullscreen 1 "$@"
