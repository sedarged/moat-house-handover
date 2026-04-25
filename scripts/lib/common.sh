#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
LOCAL_DOTNET="$ROOT_DIR/.dotnet/dotnet"

resolve_dotnet() {
  if command -v dotnet >/dev/null 2>&1; then
    echo "dotnet"
    return 0
  fi

  if [ -x "$LOCAL_DOTNET" ]; then
    echo "$LOCAL_DOTNET"
    return 0
  fi

  return 1
}

require_dotnet() {
  local dotnet_cmd
  if dotnet_cmd="$(resolve_dotnet)"; then
    echo "$dotnet_cmd"
    return 0
  fi

  echo "[ERROR] Missing required tool: dotnet"
  echo "[HINT] Run scripts/bootstrap-dotnet.sh to install a local SDK under ./.dotnet"
  return 1
}
