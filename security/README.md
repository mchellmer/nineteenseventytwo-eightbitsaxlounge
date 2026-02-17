# Security scanner (Trivy) CronJob

This folder provides the container image, Kubernetes manifests and release pipeline for a daily security scanner that runs Trivy and basic linters against the repository. The scanner stores the Trivy DB in a persistent volume and uploads SARIF output to GitHub Code Scanning when provided a PAT.

## Overview

The security scanner is designed to:
- Build a Docker image that contains `trivy`, `git`, `gh`, and basic linters.
- Run daily as a Kubernetes `CronJob`
- Clone the target repository, run `trivy fs` (SARIF) and simple language linters/tests, and upload SARIF to GitHub using a PAT.

## Repository Structure

- `Dockerfile` - builds the scanner image
- `scripts/run-scan.sh` - bundled scan script used by the image
- `k8s/cronjob.yaml.j2` - templated CronJob manifest (rendered by the deploy playbook)
- `k8s/persistentvolumeclaim.yaml` - PVC for Trivy DB cache
- `security-deploy.yaml` - Ansible playbook to template and apply manifests
- `Makefile` - build/test/push/deploy targets used by CI
- `version.txt` - image version used by release workflow
- `.github/workflows/security-release.yaml` - release workflow (calls reusable release-template)
- `CHANGELOG.md` - changelog for security component

## Script inputs / environment variables

The scanner script `scripts/run-scan.sh` reads the following environment variables. The example `Job` in this README uses the bolded ones.

- `CLONE_URL` (required if `GITHUB_REPOSITORY` is not set)
  - Full clone URL for the repository to scan (e.g. `https://github.com/mchellmer/nineteenseventytwo-eightbitsaxlounge.git`). If omitted, `GITHUB_REPOSITORY` is used to construct the clone URL.
- `GITHUB_REPOSITORY` (optional; used when `CLONE_URL` is not provided)
  - Owner/repo (e.g. `mchellmer/nineteenseventytwo-eightbitsaxlounge`). Also used as the target for SARIF upload when `GITHUB_PAT` is provided.
- `GITHUB_PAT` (optional; required to upload SARIF to GitHub Code Scanning)
  - Personal Access Token provided via secret; when present the script will upload generated SARIF files to the GitHub Code Scanning API. The script will attempt to detect the local `commit_sha`/`ref` automatically — you can override with the envs below.
- `COMMIT_SHA` (optional)
  - Override for the commit SHA to associate SARIF uploads with. If unset the script will attempt to detect `HEAD` in the cloned repo. If the commit SHA cannot be determined the SARIF upload is skipped.
- `REF` (optional)
  - Override for the git ref (e.g. `refs/heads/main`). If unset the script will attempt to detect the current branch and fallback to `refs/heads/main`.
- `IMAGES` (optional)
  - Comma- or space-separated list of container images to scan (e.g. `ghcr.io/<owner>/eightbitsaxlounge-ui:latest,ghcr.io/<owner>/eightbitsaxlounge-db:latest`). Image SARIF files are written to the output directory as `trivy-image-<sanitized-name>.sarif`.
- `MAP_IMAGE_SARIF_TO_REPO` (optional; default: `false`)
  - When `true` the script will add a synthetic repository `artifactLocation.uri` to image SARIF results (for example `security/image-scan-<image>.txt`) so GitHub Code Scanning can create repository alerts for image findings. This is opt-in because it creates synthetic file paths in alerts.

Outputs and behavior notes:
- Filesystem SARIF: written to `/workspace/output/trivy-results.sarif`.
- Image SARIFs: written to `/workspace/output/trivy-image-*.sarif`.
- If `GITHUB_PAT` is provided the script will upload any SARIF files and include `commit_sha` + `ref` to allow GitHub to create Code Scanning alerts.
- The script runs additional checks automatically when relevant files are present (Go `go vet`/`go test`, Python `flake8`, .NET `dotnet build`).

## Quickstart — build and run locally

Build the scanner image locally (from `security/`):

```bash
docker build -t ghcr.io/<owner>/eightbitsaxlounge-security:$(cat version.txt) .
```

Run a one-off scan locally (mount a cache dir to avoid DB downloads):

```bash
mkdir -p ~/.cache/trivy
docker run --rm -e CLONE_URL="https://github.com/<owner>/<repo>.git" \
  -v ~/.cache/trivy:/var/lib/trivy \
  -v $(pwd):/workspace \
  ghcr.io/<owner>/eightbitsaxlounge-security:$(cat version.txt)
```

This will clone the repository into `/workspace/repo`, run `trivy fs` and linters, and place output under `/workspace/output`.

## CI / Release

The repository contains `.github/workflows/security-release.yaml` which calls the shared `release-template.yaml`. The release flow:

- Reads `security/version.txt` for the version
- Builds the image as `ghcr.io/<owner>/eightbitsaxlounge-security:<version>` and `:latest`
- Pushes the image to GHCR
- Calls `make deploy` in `security/` which runs `ansible-playbook security-deploy.yaml` to create the `security-scan-secret` and apply manifests

The workflow expects a repository secret `SECURITY_GITHUB_PAT` (used to create the in-cluster `security-scan-secret`).

## Running manually in-cluster (one-off job)

If you'd like to execute a single run in the cluster for testing without waiting for CronJob schedule, create a Job that uses the same image and mounts the PVC:

```bash
cat <<'EOF' | kubectl apply -n eightbitsaxlounge-dev -f -
apiVersion: batch/v1
kind: Job
metadata:
  name: security-scan-once
spec:
  template:
    spec:
      containers:
      - name: scanner
        image: ghcr.io/mchellmer/eightbitsaxlounge-security:latest
        command: ["/scripts/run-scan.sh"]
        env:
        - name: CLONE_URL
          value: "https://github.com/mchellmer/nineteenseventytwo-eightbitsaxlounge.git"
        - name: GITHUB_REPOSITORY
          value: "mchellmer/nineteenseventytwo-eightbitsaxlounge"
        - name: GITHUB_PAT
          valueFrom:
            secretKeyRef:
              name: security-scan-secret
              key: github_pat
        - name: IMAGES
          value: "ghcr.io/mchellmer/eightbitsaxlounge-data:latest,ghcr.io/mchellmer/eightbitsaxlounge-midi:latest,ghcr.io/mchellmer/eightbitsaxlounge-db:latest,ghcr.io/mchellmer/eightbitsaxlounge-ui:latest"
        - name: MAP_IMAGE_SARIF_TO_REPO
          value: "true"
        # Optional: pin SARIF to a specific commit/ref (uncomment to use)
        # - name: COMMIT_SHA
        #   value: "0123456789abcdef0123456789abcdef01234567"
        # - name: REF
        #   value: "refs/heads/main"
        volumeMounts:
        - name: trivy-cache
          mountPath: /var/lib/trivy
        - name: workspace
          mountPath: /workspace
      restartPolicy: Never
      volumes:
      - name: trivy-cache
        persistentVolumeClaim:
          claimName: trivy-cache-pvc
      - name: workspace
        emptyDir: {}
EOF
```
