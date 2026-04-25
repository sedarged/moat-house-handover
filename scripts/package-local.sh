#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
source "$ROOT_DIR/scripts/lib/common.sh"
HOST_PROJECT="$ROOT_DIR/desktop-host/MoatHouseHandover.Host.csproj"
DIST_DIR="$ROOT_DIR/dist/local-host"

DOTNET_CMD="$(require_dotnet)"

if [ ! -f "$HOST_PROJECT" ]; then
  echo "[ERROR] Host project not found: $HOST_PROJECT"
  exit 1
fi

rm -rf "$DIST_DIR"
mkdir -p "$DIST_DIR"

echo "Publishing desktop host package to $DIST_DIR"
"$DOTNET_CMD" publish "$HOST_PROJECT" -c Release -o "$DIST_DIR" -p:EnableWindowsTargeting=true

echo "Local package created: $DIST_DIR"
