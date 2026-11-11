#!/usr/bin/env bash
set -euo pipefail
portlab_root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
portlab_player="$portlab_root/Builds/OperationalLinux/PortLabOperations.x86_64"
if [[ "${1:-}" == "--research" ]]; then
  shift
  portlab_player="$portlab_root/Builds/Linux/PortLab.x86_64"
elif [[ "${1:-}" == "--iteration1" ]]; then
  shift
  portlab_player="$portlab_root/Builds/OperationalIteration1Linux/PortLabOperations.x86_64"
elif [[ "${1:-}" == "--iteration2" ]]; then
  shift
  portlab_player="$portlab_root/Builds/OperationalIteration2Linux/PortLabOperations.x86_64"
elif [[ ! -x "$portlab_player" ]]; then
  portlab_player="$portlab_root/Builds/Linux/PortLab.x86_64"
fi
exec "$portlab_player" -force-vulkan -screen-fullscreen 1 "$@"
