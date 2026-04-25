#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
source "$ROOT_DIR/scripts/lib/common.sh"
HOST_PROJECT="$ROOT_DIR/desktop-host/MoatHouseHandover.Host.csproj"

DOTNET_CMD="$(require_dotnet)"

if [ ! -f "$HOST_PROJECT" ]; then
  echo "[ERROR] Host project not found: $HOST_PROJECT"
  exit 1
fi

echo "Building desktop host with Windows targeting enabled"
"$DOTNET_CMD" build "$HOST_PROJECT" -c Release -p:EnableWindowsTargeting=true

echo "Host build completed."
