# Variables
VERSION ?= $(shell cat version.txt)
NAMESPACE ?= eightbitsaxlounge-dev
IMAGE_BASE ?= ghcr.io/$(GITHUB_REPOSITORY_OWNER)/eightbitsaxlounge-couchdb-api
GITHUB_REPOSITORY_OWNER ?= mchellmer

# Lint Go code
lint:
	@echo "Running Go linter..."
	docker run --rm -v "$(shell pwd):/data" -w /data/go golangci/golangci-lint:latest golangci-lint run ./...

# Run Go tests in container
test:
	@echo "Running Go tests..."
	docker run --rm -v "$(shell pwd):/data" -w /data/go golang:1.24-alpine go test ./...
	@echo "All tests passed"

# Build Docker image and push to registry
build:
	docker build -t "$(IMAGE_BASE):$(VERSION)" -t "$(IMAGE_BASE):latest" .
	docker push "$(IMAGE_BASE):$(VERSION)"
	docker push "$(IMAGE_BASE):latest"

# Deploy to Kubernetes via Ansible
deploy:
	ansible-playbook data-go-couchdb.yaml

# Legacy targets for backwards compatibility
deploy-data-api: deploy

docker-build:
	docker build -t eightbitsaxlounge-data .

docker-run:
	docker run -d --name eightbitsaxlounge-data -p 8080:8080 eightbitsaxlounge-data

# Help target
help:
	@echo "Available targets:"
	@echo "  test               - Run Go tests in container"
	@echo "  build              - Build and push Docker image"
	@echo "  deploy             - Deploy to Kubernetes cluster"
	@echo "  deploy-data-api    - (deprecated) Same as deploy"
	@echo ""
	@echo "Environment variables:"
	@echo "  VERSION            - Image version (default: from version.txt)"
	@echo "  NAMESPACE          - K8s namespace (default: eightbitsaxlounge-dev)"
	@echo "  GITHUB_REPOSITORY_OWNER - Docker registry owner (default: mchellmer)"

.PHONY: test build deploy deploy-data-api docker-build docker-run help