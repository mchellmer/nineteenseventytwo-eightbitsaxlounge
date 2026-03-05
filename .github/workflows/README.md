# CI/CD Pattern: Release Template Workflow

This repository uses a standardized CI/CD pattern where GitHub Actions workflows handle orchestration and Make targets contain implementation logic.

## Architecture

```
Component Workflow → Release Template → Make Targets → Ansible
```

## Release Template

**Location**: `.github/workflows/release-template.yaml`

**Key Inputs**: `component`, `test-target`, `build-target`, `deploy-target`, `component-secrets` (JSON)

**Jobs**: Test (optional) → Build → Deploy

## Makefile Assumptions

Required targets: `build`, `deploy`, `help`

Standard environment variables: `VERSION`, `NAMESPACE`, `GITHUB_REPOSITORY_OWNER`
