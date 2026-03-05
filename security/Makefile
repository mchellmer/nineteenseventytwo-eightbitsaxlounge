# CI/CD Variables
VERSION ?= $(shell cat version.txt)
NAMESPACE ?= eightbitsaxlounge-dev
GITHUB_REPOSITORY_OWNER ?= mchellmer
IMAGE_BASE = ghcr.io/$(GITHUB_REPOSITORY_OWNER)/eightbitsaxlounge-security

.PHONY: test build-image test-image push deploy help

help:
	@echo "Security - Build and Deployment Targets"
	@echo "  test             - Validate Dockerfile and version.txt"
	@echo "  build-image      - Build Docker image locally (no push)"
	@echo "  test-image       - Verify built image exists"
	@echo "  push             - Push image to registry"
	@echo "  deploy           - Deploy security scanner to Kubernetes (via Ansible)"

test:
	@echo "Running validation tests..."
	@test -f Dockerfile || (echo "ERROR: Dockerfile not found" && exit 1)
	@test -f version.txt || (echo "ERROR: version.txt not found" && exit 1)
	@echo "✓ Dockerfile exists"
	@echo "✓ version.txt exists ($(VERSION))"

build-image:
	@echo "Building image $(IMAGE_BASE):$(VERSION)..."
	docker build -t "$(IMAGE_BASE):$(VERSION)" -t "$(IMAGE_BASE):latest" .
	@echo "✓ Image built successfully"

test-image:
	@echo "Testing built image..."
	@docker image inspect "$(IMAGE_BASE):$(VERSION)" > /dev/null || (echo "ERROR: Image not built" && exit 1)
	@echo "✓ Image exists and is valid"

push:
	@echo "Pushing image $(IMAGE_BASE):$(VERSION) to registry..."
	docker push "$(IMAGE_BASE):$(VERSION)"
	docker push "$(IMAGE_BASE):latest"
	@echo "✓ Image pushed successfully"

deploy:
	@echo "Deploying security scanner to Kubernetes namespace: $(NAMESPACE)..."
	ansible-playbook security-deploy.yaml -vv
