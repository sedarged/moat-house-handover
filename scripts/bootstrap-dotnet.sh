#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
DOTNET_DIR="$ROOT_DIR/.dotnet"
INSTALL_SCRIPT="$ROOT_DIR/.tmp/dotnet-install.sh"
CHANNEL="${1:-8.0}"

mkdir -p "$ROOT_DIR/.tmp" "$DOTNET_DIR"

if [ -x "$DOTNET_DIR/dotnet" ]; then
  echo "[OK] Local dotnet already installed: $DOTNET_DIR/dotnet"
  "$DOTNET_DIR/dotnet" --version
  exit 0
fi

if command -v curl >/dev/null 2>&1; then
  curl -fsSL https://dot.net/v1/dotnet-install.sh -o "$INSTALL_SCRIPT"
elif command -v wget >/dev/null 2>&1; then
  wget -qO "$INSTALL_SCRIPT" https://dot.net/v1/dotnet-install.sh
else
  echo "[ERROR] Missing both curl and wget; cannot download dotnet-install.sh"
  exit 1
fi

chmod +x "$INSTALL_SCRIPT"
"$INSTALL_SCRIPT" --channel "$CHANNEL" --install-dir "$DOTNET_DIR"

echo "[OK] Installed local dotnet SDK in $DOTNET_DIR"
"$DOTNET_DIR/dotnet" --version
