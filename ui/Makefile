# Variables
IMAGE_NAME := nineteenseventytwo/eightbitsaxlounge-ui
VERSION := $(shell cat version.txt)
REGISTRY := docker.io

# Docker targets
.PHONY: build push clean test run

build:
	docker build -t $(IMAGE_NAME):$(VERSION) .
	docker tag $(IMAGE_NAME):$(VERSION) $(IMAGE_NAME):latest

push:
	docker push $(IMAGE_NAME):$(VERSION)
	docker push $(IMAGE_NAME):latest

run:
	docker run --rm -it \
		-e TWITCH_TOKEN=$(TWITCH_TOKEN) \
		-e TWITCH_CLIENT_ID=$(TWITCH_CLIENT_ID) \
		-e TWITCH_CHANNEL=$(TWITCH_CHANNEL) \
		-e MIDI_API_URL=$(MIDI_API_URL) \
		-p 8080:8080 \
		$(IMAGE_NAME):$(VERSION)

# Development targets
.PHONY: install dev test-local

install:
	pip install -r requirements.txt

install-dev:
	pip install -r requirements.txt
	pip install -r requirements-dev.txt

dev:
	python src/main.py

# Testing targets
.PHONY: test test-unit test-integration test-coverage test-watch test-ci

test: install-dev
	python -m pytest tests/ -v

test-unit:
	python -m pytest tests/test_handlers.py tests/test_commands.py tests/test_config.py -v

test-integration:
	python -m pytest tests/test_main.py tests/test_health_check.py -v

test-coverage: install-dev
	python -m pytest tests/ --cov=src --cov-report=html --cov-report=term-missing --cov-fail-under=80

test-watch: install-dev
	python -m pytest tests/ --watch

test-ci:
	python -m pytest tests/ --cov=src --cov-report=xml --cov-report=term --junitxml=test-results.xml

test-fast:
	python -m pytest tests/ -v --tb=short --disable-warnings

# Kubernetes targets
.PHONY: deploy undeploy restart

deploy:
	kubectl apply -f k8s/

undeploy:
	kubectl delete -f k8s/

restart:
	kubectl rollout restart deployment/ui-deployment

# Clean up
clean:
	docker rmi $(IMAGE_NAME):$(VERSION) $(IMAGE_NAME):latest || true
	docker system prune -f

clean-test:
	rm -rf .pytest_cache/
	rm -rf htmlcov/
	rm -rf .coverage
	rm -f test-results.xml
	rm -rf __pycache__/
	find . -name "*.pyc" -delete
	find . -name "*__pycache__*" -type d -exec rm -rf {} + || true

# Code quality targets
.PHONY: lint format check-format

lint:
	flake8 src/ tests/ || echo "flake8 not installed, skipping"
	pylint src/ || echo "pylint not installed, skipping"

format:
	black src/ tests/ || echo "black not installed, skipping"
	isort src/ tests/ || echo "isort not installed, skipping"

check-format:
	black --check src/ tests/ || echo "black not installed, skipping"
	isort --check-only src/ tests/ || echo "isort not installed, skipping"

# Version management
.PHONY: version-patch version-minor version-major

version-patch:
	@echo "$(shell echo $(VERSION) | awk -F. '{print $$1"."$$2"."$$3+1}')" > version.txt
	@echo "Updated version to $(shell cat version.txt)"

version-minor:
	@echo "$(shell echo $(VERSION) | awk -F. '{print $$1"."$$2+1".0"}')" > version.txt
	@echo "Updated version to $(shell cat version.txt)"

version-major:
	@echo "$(shell echo $(VERSION) | awk -F. '{print $$1+1".0.0"}')" > version.txt
	@echo "Updated version to $(shell cat version.txt)"