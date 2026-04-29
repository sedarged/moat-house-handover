#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"

if command -v npm >/dev/null 2>&1; then
  npm install
  npx playwright install --with-deps chromium
else
  echo "npm is not available; UI harness setup skipped." >&2
fi

if [ -x "./scripts/bootstrap-dotnet.sh" ]; then
  ./scripts/bootstrap-dotnet.sh || true
fi

if [ -x "./scripts/check-prereqs.sh" ]; then
  ./scripts/check-prereqs.sh || true
fi

if [ -x "./scripts/check-web.sh" ]; then
  ./scripts/check-web.sh
fi
