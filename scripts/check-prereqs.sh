#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
source "$ROOT_DIR/scripts/lib/common.sh"

require_cmd() {
  local cmd="$1"
  if ! command -v "$cmd" >/dev/null 2>&1; then
    echo "[ERROR] Missing required tool: $cmd"
    return 1
  fi
  echo "[OK] Found $cmd: $(command -v "$cmd")"
}

echo "Checking prerequisites in $ROOT_DIR"
require_cmd git
require_cmd node

dotnet_cmd="$(require_dotnet)"
echo "[OK] Found dotnet: $dotnet_cmd"
"$dotnet_cmd" --version

echo "All required tools are available."
