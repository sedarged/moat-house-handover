#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
WEBAPP_DIR="$ROOT_DIR/webapp"

if ! command -v node >/dev/null 2>&1; then
  echo "[ERROR] Missing required tool: node"
  exit 1
fi

if [ ! -f "$WEBAPP_DIR/index.html" ]; then
  echo "[ERROR] Missing web entrypoint: $WEBAPP_DIR/index.html"
  exit 1
fi

echo "[OK] Found web entrypoint: $WEBAPP_DIR/index.html"

mapfile -t js_files < <(find "$WEBAPP_DIR/js" -type f -name '*.js' | sort)

if [ "${#js_files[@]}" -eq 0 ]; then
  echo "[ERROR] No JavaScript files found under $WEBAPP_DIR/js"
  exit 1
fi

echo "Running syntax checks with node --check"
for file in "${js_files[@]}"; do
  node --check "$file" >/dev/null
  echo "[OK] $file"
done

echo "Web checks completed."
