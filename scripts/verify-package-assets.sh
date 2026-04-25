#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PACKAGE_DIR="${1:-$ROOT_DIR/dist/local-host}"

if [ ! -d "$PACKAGE_DIR" ]; then
  echo "[ERROR] Package directory not found: $PACKAGE_DIR"
  echo "Run scripts/package-local.sh first."
  exit 1
fi

WEB_INDEX="$PACKAGE_DIR/webapp/index.html"
RUNTIME_CONFIG="$PACKAGE_DIR/config/runtime.config.json"

if [ ! -f "$WEB_INDEX" ]; then
  echo "[ERROR] Missing packaged web asset: $WEB_INDEX"
  exit 1
fi

if [ ! -f "$RUNTIME_CONFIG" ]; then
  echo "[ERROR] Missing packaged runtime config: $RUNTIME_CONFIG"
  exit 1
fi

echo "[OK] Found packaged web asset: $WEB_INDEX"
echo "[OK] Found packaged runtime config: $RUNTIME_CONFIG"
echo "Packaged asset verification passed."
