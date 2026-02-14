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
        image: ghcr.io/mchellmer/eightbitsaxlounge-security:0.0.1
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

## Notes & Best Practices

- Pre-warm the Trivy DB into the PVC to avoid long download times on first runs (see repo root notes). A scheduled job in the default branch can periodically update the cache.
- Use the `SECURITY_GITHUB_PAT` repository secret with least privilege required for SARIF uploads.
