# CI/CD Pattern: Release Template Workflow

This repository uses a standardized CI/CD pattern for releasing components. The pattern separates concerns between GitHub Actions workflows (orchestration) and Make targets (implementation).

## Architecture

```
GitHub Workflow (db-release.yaml)
  └─> Release Template (release-template.yaml)
       ├─> Test Job → make test (optional)
       ├─> Build Job → make build
       │                make package (optional)
       └─> Deploy Job → make deploy
                         └─> Ansible Playbook
```

## Pattern Benefits

1. **Consistency**: All components follow the same workflow structure
2. **Reusability**: Workflow template handles common orchestration logic
3. **Portability**: Make targets can be run locally or in any CI system
4. **Separation**: GitHub-specific actions in workflow, implementation in Makefile
5. **Flexibility**: Component-specific behavior via inputs and secrets

## Release Template Workflow

**Location**: `.github/workflows/release-template.yaml`

**Inputs**:
- `component`: Directory name (db, data, midi, ui)
- `version-file`: Path to version file (default: version.txt)
- `build-target`: Make target for build (default: build)
- `test-target`: Make target for tests (optional)
- `package-target`: Make target for packaging (optional)
- `deploy-target`: Make target for deployment (required)
- `requires-docker-login`: Whether to authenticate with GHCR (default: true)

**Secrets**:
- `component-secrets`: JSON object of component-specific secrets passed as environment variables

**Jobs**:
1. **Test**: Runs `make <test-target>` if specified
2. **Build**: Reads version, sets namespace, runs `make <build-target>` and optional `make <package-target>`
3. **Deploy**: Runs `make <deploy-target>` with version, namespace, and component secrets

## Component Structure

Each component should have:

```
component/
├── version.txt              # Semantic version
├── Makefile                 # Standardized targets
├── CHANGELOG.md             # Version history
├── README.md                # Component documentation
├── <component>.yaml         # Ansible playbook (if applicable)
└── k8s/                     # Kubernetes manifests (if applicable)
```

### Required Makefile Targets

- `build`: Build and/or push artifacts (images, binaries, etc.)
- `deploy`: Deploy to target environment (typically via Ansible)
- `help`: Display available targets and environment variables

### Optional Makefile Targets

- `test`: Run unit/integration tests
- `package`: Package artifacts for distribution

### Environment Variables

Makefile targets should accept these standard variables:
- `VERSION`: Component version (from version.txt)
- `NAMESPACE`: Kubernetes namespace (eightbitsaxlounge-dev or eightbitsaxlounge-prod)
- `GITHUB_REPOSITORY_OWNER`: Docker registry owner
- Component-specific secrets (e.g., `COUCHDB_PASSWORD`)

## Example: DB Component

**Workflow** (`.github/workflows/db-release.yaml`):
```yaml
jobs:
  release:
    uses: ./.github/workflows/release-template.yaml
    with:
      component: 'db'
      test-target: 'test'
      build-target: 'build'
      deploy-target: 'deploy'
    secrets:
      component-secrets: |
        {
          "COUCHDB_PASSWORD": "${{ secrets.DB_COUCHDB_PASSWORD }}"
        }
```

**Makefile** (`db/Makefile`):
```makefile
VERSION ?= $(shell cat version.txt)
NAMESPACE ?= eightbitsaxlounge-dev

build:
	docker build -t "$(IMAGE_BASE):$(VERSION)" .
	docker push "$(IMAGE_BASE):$(VERSION)"

deploy:
	ansible-playbook db-couchdb.yaml
```

## Local Development

Run the same targets locally:

```bash
# Build
cd db
make build VERSION=1.0.1

# Deploy to dev
make deploy NAMESPACE=eightbitsaxlounge-dev COUCHDB_PASSWORD=secret

# Deploy to prod
make deploy NAMESPACE=eightbitsaxlounge-prod COUCHDB_PASSWORD=secret
```

## Migration Guide

To migrate existing components to this pattern:

1. **Update Makefile**: Add standard targets (build, deploy, help) with environment variable support
2. **Create new workflow**: Copy template and customize inputs/secrets
3. **Test**: Run locally and via workflow
4. **Switch**: Update trigger paths to use new workflow file
5. **Cleanup**: Archive old workflow after verification

## Future Enhancements

- Add `lint` and `validate` targets
- Support matrix builds for multi-platform images
- Add artifact upload/download between jobs
- Implement rollback mechanism
- Add smoke tests post-deployment
