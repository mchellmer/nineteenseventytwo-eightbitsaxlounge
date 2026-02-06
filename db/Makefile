# Variables
VERSION ?= $(shell cat version.txt)
NAMESPACE ?= eightbitsaxlounge-dev
IMAGE_BASE ?= ghcr.io/$(GITHUB_REPOSITORY_OWNER)/eightbitsaxlounge-couchdb
GITHUB_REPOSITORY_OWNER ?= mchellmer

# Validate Dockerfile and version
test:
	@echo "Running validation tests..."
	@test -f Dockerfile || (echo "ERROR: Dockerfile not found" && exit 1)
	@test -f version.txt || (echo "ERROR: version.txt not found" && exit 1)
	@grep -E '^[0-9]+\.[0-9]+\.[0-9]+$$' version.txt > /dev/null || (echo "ERROR: Invalid version format in version.txt" && exit 1)
	@echo "✓ Dockerfile exists"
	@echo "✓ version.txt exists and is valid ($(VERSION))"
	@echo "All tests passed"

# Build Docker image and push to registry
build:
	docker build -t "$(IMAGE_BASE):$(VERSION)" -t "$(IMAGE_BASE):latest" .
	docker push "$(IMAGE_BASE):$(VERSION)"
	docker push "$(IMAGE_BASE):latest"

# Deploy to Kubernetes via Ansible
deploy:
	ansible-playbook db-couchdb.yaml

# Legacy target for backwards compatibility
deploy-couchdb: deploy

# Help target
help:
	@echo "Available targets:"
	@echo "  test               - Validate Dockerfile and version.txt"
	@echo "  build              - Build and push Docker image"
	@echo "  deploy             - Deploy to Kubernetes cluster"
	@echo "  deploy-couchdb     - (deprecated) Same as deploy"
	@echo ""
	@echo "Environment variables:"
	@echo "  VERSION            - Image version (default: from version.txt)"
	@echo "  NAMESPACE          - K8s namespace (default: eightbitsaxlounge-dev)"
	@echo "  GITHUB_REPOSITORY_OWNER - Docker registry owner (default: mchellmer)"
	@echo "  COUCHDB_PASSWORD   - CouchDB admin password (required for deploy)"

.PHONY: test build deploy deploy-couchdb help
