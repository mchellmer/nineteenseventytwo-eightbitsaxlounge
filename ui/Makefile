# Variables
VERSION ?= $(shell cat version.txt)
NAMESPACE ?= eightbitsaxlounge-dev
IMAGE_BASE ?= ghcr.io/$(GITHUB_REPOSITORY_OWNER)/eightbitsaxlounge-ui
GITHUB_REPOSITORY_OWNER ?= mchellmer

# Lint Python code
lint:
	@echo "Running Python linter..."
	pip3 install ruff --break-system-packages || pip3 install ruff
	ruff check src/ tests/

# Run Python tests with coverage
test:
	@echo "Running Python tests..."
	pip3 install -r requirements.txt --break-system-packages || pip3 install -r requirements.txt
	pip3 install -r requirements-dev.txt --break-system-packages || pip3 install -r requirements-dev.txt
	python3 -m pytest tests/ --cov=src --cov-report=xml --cov-report=term --junitxml=test-results.xml
	@echo "All tests passed"

# Build Docker image and push to registry
build:
	docker build -t "$(IMAGE_BASE):$(VERSION)" -t "$(IMAGE_BASE):latest" .
	docker push "$(IMAGE_BASE):$(VERSION)"
	docker push "$(IMAGE_BASE):latest"

# Deploy to Kubernetes via Ansible
deploy:
	ansible-playbook ui-deploy.yaml

# Legacy targets for backwards compatibility
deploy-ui: deploy

# Local development targets
install:
	pip3 install -r requirements.txt

install-dev:
	pip3 install -r requirements.txt
	pip3 install -r requirements-dev.txt

dev:
	python3 src/main.py

test-local: install-dev
	python3 -m pytest tests/ -v

clean:
	rm -rf .pytest_cache/
	rm -rf htmlcov/
	rm -rf .coverage
	rm -f test-results.xml
	rm -rf __pycache__/
	find . -name "*.pyc" -delete
	find . -name "*__pycache__*" -type d -exec rm -rf {} + 2>/dev/null || true

docker-build:
	docker build -t eightbitsaxlounge-ui:local .

docker-run:
	docker run --rm -it --env-file .env eightbitsaxlounge-ui:local

set-environment:
	ansible-playbook ui-set-environment.yaml

# Help target
help:
	@echo "Available targets:"
	@echo "  test               - Run tests with coverage (for CI)"
	@echo "  build              - Build and push Docker image"
	@echo "  deploy             - Deploy to Kubernetes cluster"
	@echo "  deploy-ui          - (deprecated) Same as deploy"
	@echo ""
	@echo "Local development:"
	@echo "  install            - Install Python dependencies"
	@echo "  install-dev        - Install dev dependencies"
	@echo "  dev                - Run bot locally"
	@echo "  test-local         - Run tests (verbose)"
	@echo "  clean              - Clean test artifacts"
	@echo "  docker-build       - Build Docker image locally"
	@echo "  docker-run         - Run Docker image locally"
	@echo "  set-environment    - Switch active environment (dev/prod)"
	@echo ""
	@echo "Environment variables:"
	@echo "  VERSION            - Image version (default: from version.txt)"
	@echo "  NAMESPACE          - K8s namespace (default: eightbitsaxlounge-dev)"
	@echo "  GITHUB_REPOSITORY_OWNER - Docker registry owner (default: mchellmer)"

.PHONY: test build deploy deploy-ui install install-dev dev test-local clean docker-build docker-run set-environment help