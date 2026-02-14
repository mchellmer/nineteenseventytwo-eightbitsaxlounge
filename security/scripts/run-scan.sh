#!/usr/bin/env bash
set -euo pipefail

WORKDIR=/workspace
REPO_DIR=${WORKDIR}/repo
OUTDIR=${WORKDIR}/output
mkdir -p "$REPO_DIR" "$OUTDIR"

# Determine clone URL: prefer CLONE_URL, then GITHUB_REPOSITORY, else fail
if [ -n "${CLONE_URL:-}" ]; then
  URL="$CLONE_URL"
elif [ -n "${GITHUB_REPOSITORY:-}" ]; then
  URL="https://github.com/${GITHUB_REPOSITORY}.git"
else
  echo "Neither CLONE_URL nor GITHUB_REPOSITORY set. Set CLONE_URL to the repo to scan." >&2
  exit 1
fi

echo "Cloning ${URL}..."
git clone --depth 1 "$URL" "$REPO_DIR"

echo "Running Trivy filesystem scan (SARIF)..."
trivy fs --format sarif --output "$OUTDIR/trivy-results.sarif" "$REPO_DIR" || true

echo "Running simple linters/tests..."
if [ -f "$REPO_DIR/go.mod" ]; then
  (cd "$REPO_DIR" && go vet ./... || true)
  (cd "$REPO_DIR" && go test ./... || true)
fi

if compgen -G "$REPO_DIR"/*.py >/dev/null 2>&1 || [ -f "$REPO_DIR/requirements.txt" ]; then
  if command -v flake8 >/dev/null 2>&1; then
    (cd "$REPO_DIR" && flake8 . || true)
  else
    echo "flake8 not installed; skipping python lint"
  fi
fi

if compgen -G "$REPO_DIR"/*.csproj >/dev/null 2>&1; then
  if command -v dotnet >/dev/null 2>&1; then
    (cd "$REPO_DIR" && dotnet build || true)
  else
    echo "dotnet not installed; skipping dotnet build"
  fi
fi

# Optionally upload SARIF to GitHub Code Scanning API if GITHUB_PAT provided
if [ -n "${GITHUB_PAT:-}" ]; then
  echo "Preparing SARIF upload..."
  SARIF_PATH="${OUTDIR}/trivy-results.sarif"
  if [ ! -f "$SARIF_PATH" ]; then
    echo "SARIF file not found at $SARIF_PATH; skipping upload"
  else
    # determine owner/repo
    OWNER_REPO="${GITHUB_REPOSITORY:-}"
    if [ -z "$OWNER_REPO" ]; then
      OWNER_REPO=$(echo "$URL" | sed -E 's#https?://[^/]+/([^/]+/[^/]+)(\.git)?#\1#')
    fi

    # commit/ref detection: allow env override, else use repo HEAD
    COMMIT_SHA="${COMMIT_SHA:-}"
    REF="${REF:-}"
    if [ -z "$COMMIT_SHA" ] || [ -z "$REF" ]; then
      if [ -d "$REPO_DIR/.git" ]; then
        COMMIT_SHA=$(git -C "$REPO_DIR" rev-parse --verify HEAD 2>/dev/null || echo "")
        BRANCH=$(git -C "$REPO_DIR" symbolic-ref --quiet --short HEAD 2>/dev/null || echo "")
        if [ -n "$BRANCH" ]; then
          REF="refs/heads/$BRANCH"
        else
          REF="refs/heads/main"
        fi
      else
        REF="refs/heads/main"
      fi
    fi

    if [ -z "$COMMIT_SHA" ]; then
      echo "Warning: commit SHA unknown. Upload requires a commit_sha; skipping upload." >&2
    else
      echo "GZipping and base64-encoding SARIF..."
      SARIF_B64=$(gzip -c "$SARIF_PATH" 2>/dev/null | base64 | tr -d '\n')

      echo "Building payload..."
      if command -v jq >/dev/null 2>&1; then
        PAYLOAD=$(jq -nc --arg commit_sha "$COMMIT_SHA" --arg ref "$REF" --arg sarif "$SARIF_B64" '{commit_sha:$commit_sha, ref:$ref, sarif:$sarif}')
      else
        # fallback to python to construct payload
        PAYLOAD=$(python3 - <<PY
import json,os,sys
payload={
  'commit_sha': os.environ.get('COMMIT_SHA', '${COMMIT_SHA}'),
  'ref': os.environ.get('REF', '${REF}'),
  'sarif': os.environ.get('SARIF_B64', '${SARIF_B64}')
}
print(json.dumps(payload))
PY
)
      fi

      echo "Uploading SARIF to GitHub..."
      RESPONSE=$(curl -s -H "Accept: application/vnd.github+json" -H "Authorization: Bearer ${GITHUB_PAT}" -H "Content-Type: application/json" -d "$PAYLOAD" "https://api.github.com/repos/${OWNER_REPO}/code-scanning/sarifs") || true
      echo "$RESPONSE" | jq -C . || echo "$RESPONSE"
    fi
  fi
fi

echo "Scan complete; results in $OUTDIR"
