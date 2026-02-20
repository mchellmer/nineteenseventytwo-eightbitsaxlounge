#!/usr/bin/env bash
# Verify sample image assets required for the overlay demo are present on disk
set -euo pipefail
BASE="$(cd "$(dirname "$0")/.." && pwd)/public/images"
FILES=(
  "$BASE/engine/lofi.svg"
  "$BASE/engine/room.svg"
  "$BASE/values/0.svg"
  "$BASE/values/1.svg"
  "$BASE/values/2.svg"
  "$BASE/values/3.svg"
  "$BASE/values/4.svg"
  "$BASE/values/5.svg"
  "$BASE/values/6.svg"
  "$BASE/values/7.svg"
  "$BASE/values/8.svg"
  "$BASE/values/9.svg"
  "$BASE/values/10.svg"
  "$BASE/error.svg"
)

for f in "${FILES[@]}"; do
  if [[ -f "$f" ]]; then
    echo "OK: $f"
  else
    echo "MISSING: $f" >&2
    exit 2
  fi
done

echo "All overlay demo images present."