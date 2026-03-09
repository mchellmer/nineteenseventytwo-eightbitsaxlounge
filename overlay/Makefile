# Variables
VERSION ?= $(shell cat version.txt)
GITHUB_REPOSITORY_OWNER ?= mchellmer
IMAGE_BASE ?= ghcr.io/$(GITHUB_REPOSITORY_OWNER)/eightbitsaxlounge-overlay

# Validate source files and Ansible playbook
build:
	@echo "Validating configuration..."
	@test -f Dockerfile || (echo "ERROR: Dockerfile not found" && exit 1)
	@test -f overlay-deploy.yaml || (echo "ERROR: overlay-deploy.yaml not found" && exit 1)
	@test -f version.txt || (echo "ERROR: version.txt not found" && exit 1)
	@grep -E '^[0-9]+\.[0-9]+\.[0-9]+$$' version.txt > /dev/null || (echo "ERROR: Invalid version format in version.txt" && exit 1)
	@test -f k8s/deployment.yaml || (echo "ERROR: k8s/deployment.yaml not found" && exit 1)
	@test -f k8s/service.yaml || (echo "ERROR: k8s/service.yaml not found" && exit 1)
	@test -f k8s/ingress.yaml.j2 || (echo "ERROR: k8s/ingress.yaml.j2 not found" && exit 1)
	@echo "✓ All required files exist"
	@echo "✓ version.txt is valid ($(VERSION))"
	@ansible-playbook --syntax-check overlay-deploy.yaml > /dev/null || (echo "ERROR: Ansible syntax check failed" && exit 1)
	@echo "✓ Ansible playbook syntax is valid"
	@echo "All validation checks passed"

# Build Docker image with version tag
build-image:
	@echo "Building image $(IMAGE_BASE):$(VERSION)..."
	docker build -t "$(IMAGE_BASE):$(VERSION)" -t "$(IMAGE_BASE):latest" .
	@echo "✓ Image built: $(IMAGE_BASE):$(VERSION)"

# Verify built image exists
test-image:
	@echo "Verifying built image..."
	@docker image inspect "$(IMAGE_BASE):$(VERSION)" > /dev/null 2>&1 || (echo "ERROR: Image not built" && exit 1)
	@echo "✓ Image $(IMAGE_BASE):$(VERSION) exists"

# Push image to GHCR
push:
	@echo "Pushing $(IMAGE_BASE):$(VERSION) to registry..."
	docker push "$(IMAGE_BASE):$(VERSION)"
	docker push "$(IMAGE_BASE):latest"
	@echo "✓ Image pushed successfully"

# Run unit tests and image asset checks via Docker (no npm required on host)
test:
	@echo "Running tests in Docker (node:25-slim)..."
	docker run --rm \
		--user $$(id -u):$$(id -g) \
		-v "$(CURDIR):/app" \
		-w /app \
		node:25-slim \
		bash -c "npm ci && npm test"

# Deploy overlay to Kubernetes cluster
deploy:
	@test -n "$$NAMESPACE" || (echo "ERROR: NAMESPACE env var not set" && exit 1)
	@echo "🚀 Deploying overlay to namespace $$NAMESPACE with version $(VERSION)"
	NAMESPACE="$$NAMESPACE" VERSION="$(VERSION)" GITHUB_REPOSITORY_OWNER="$(GITHUB_REPOSITORY_OWNER)" \
		ansible-playbook overlay-deploy.yaml

# Manual deployment with interactive prompts
deploy-overlay-manual:
	@if [ -z "$$NAMESPACE" ]; then \
		echo "⚠️  Manual deployment mode: using prompts"; \
		read -p "Enter NAMESPACE (default: eightbitsaxlounge-dev): " ns; \
		NAMESPACE="$${ns:-eightbitsaxlounge-dev}"; \
		NAMESPACE="$$NAMESPACE" VERSION="$(VERSION)" GITHUB_REPOSITORY_OWNER="$(GITHUB_REPOSITORY_OWNER)" \
			ansible-playbook overlay-deploy.yaml; \
	else \
		NAMESPACE="$$NAMESPACE" VERSION="$(VERSION)" GITHUB_REPOSITORY_OWNER="$(GITHUB_REPOSITORY_OWNER)" \
			ansible-playbook overlay-deploy.yaml; \
	fi

docker-build:
	docker build -t eightbitsaxlounge-overlay:local .

docker-run:
	docker run --rm -p 3000:3000 eightbitsaxlounge-overlay:local

# Publish a test message to the local NATS (useful when you run `state make test`)
smoke:
	@echo "Publishing sample overlay.engine -> NATS (NATS_URL=$${NATS_URL:-nats://127.0.0.1:4222}, USER=$${NATS_USER:-}, PASS=$${NATS_PASS:-})"
	node scripts/publish-sample.js

# Run the integration test which publishes multiple overlay.* messages
# and leaves instructions to view the overlay grid (uses tests/test_integration.sh)
deps:
	@echo "Installing node dependencies (overlay)"
	@npm ci --omit=dev || npm install --omit=dev

test-integration: deps
	@echo "Running integration test (publishes multiple overlay.* messages)"
	@bash tests/test_integration.sh

# PowerShell version of integration test for Windows
test-integration-ps: deps
	@echo "Running integration test (publishes multiple overlay.* messages)"
	powershell -NoProfile -File tests/test_integration.ps1

# Run Jest unit tests (overlay.js + NatsOverlayService)
test-unit:
	@echo "Running unit tests"
	npm run test-unit

# verify demo image assets exist
test-images:
	@echo "Checking overlay demo image assets"
	@./tests/test_images.sh

# Podman helpers — run the overlay container locally and connect to a NATS container
podman-build:
	podman build -t eightbitsaxlounge-overlay:local .

# Run overlay (bind to localhost:3000). If NATS is running in Podman and published to host
# use NATS_URL=nats://host.containers.internal:4222 (Podman Desktop DNS mapping).
podman-run:
	podman run --rm -p 3000:3000 \
		-e NATS_URL=$${NATS_URL:-nats://host.containers.internal:4222} \
		-e NATS_USER=$${NATS_USER:-} \
		-e NATS_PASS=$${NATS_PASS:-} \
		eightbitsaxlounge-overlay:local

podman-run-ps:
	powershell -NoProfile -Command \
		"& { $$natsUrl = if ($$env:NATS_URL) { $$env:NATS_URL } else { 'nats://host.containers.internal:4222' }; \
		$$natsUser = if ($$env:NATS_USER) { $$env:NATS_USER } else { '' }; \
		$$natsPass = if ($$env:NATS_PASS) { $$env:NATS_PASS } else { '' }; \
		podman run --rm -p 3000:3000 -e NATS_URL=$$natsUrl -e NATS_USER=$$natsUser -e NATS_PASS=$$natsPass eightbitsaxlounge-overlay:local }"

# Run overlay connected to local NATS container (uses dev credentials from state/Makefile test-local)
podman-run-local:
	podman run --rm -p 3000:3000 \
		-e NATS_URL=nats://host.containers.internal:4222 \
		-e NATS_USER=overlay \
		-e NATS_PASS=overlaypw \
		eightbitsaxlounge-overlay:local

# PowerShell version for Windows
podman-run-local-ps:
	powershell -NoProfile -Command \
		"podman run --rm -p 3000:3000 -e NATS_URL=nats://host.containers.internal:4222 -e NATS_USER=overlay -e NATS_PASS=overlaypw eightbitsaxlounge-overlay:local"

# Run the smoke publisher from inside a container (connects to host NATS by default)
podman-smoke:
	podman run --rm \
		-e NATS_URL=$${NATS_URL:-nats://host.containers.internal:4222} \
		-e NATS_USER=$${NATS_USER:-} \
		-e NATS_PASS=$${NATS_PASS:-} \
		eightbitsaxlounge-overlay:local npm run smoke

.PHONY: build build-image test-image push test deploy deploy-overlay-manual docker-build docker-run smoke deps test-integration test-integration-ps test-unit test-images podman-build podman-run podman-run-ps podman-run-local podman-run-local-ps podman-smoke