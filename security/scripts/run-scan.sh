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
  echo "Uploading SARIF to GitHub Code Scanning API..."
  OWNER_REPO="${GITHUB_REPOSITORY:-}"
  if [ -z "$OWNER_REPO" ]; then
    # try to extract from URL
    OWNER_REPO=$(basename -s .git "${URL#*://*/}")
  fi
  COMMIT_SHA=$(git -C "$REPO_DIR" rev-parse --verify HEAD || echo "")
  if [ -n "$COMMIT_SHA" ]; then
    curl -sSL -X POST \
      -H "Authorization: token ${GITHUB_PAT}" \
      -F "sarif=@${OUTDIR}/trivy-results.sarif" \
      -F "commit_sha=${COMMIT_SHA}" \
      "https://api.github.com/repos/${OWNER_REPO}/code-scanning/sarifs" || true
  else
    echo "No commit SHA; skipping SARIF upload"
  fi
else
  echo "GITHUB_PAT not set; skipping SARIF upload"
fi

echo "Scan complete; results in $OUTDIR"
