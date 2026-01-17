# Simple Makefile for local development
# CI/CD handled by GitHub Actions

.PHONY: help install install-dev dev test test-ci clean docker-build docker-run

help:
	@echo "Available targets:"
	@echo "  install      - Install Python dependencies"
	@echo "  install-dev  - Install dev dependencies"
	@echo "  dev          - Run bot locally"
	@echo "  test         - Run all tests"
	@echo "  test-ci      - Run tests with coverage (for CI)"
	@echo "  clean        - Clean test artifacts"
	@echo "  docker-build - Build Docker image locally"
	@echo "  docker-run   - Run Docker image locally"

install:
	pip3 install -r requirements.txt

install-dev:
	pip3 install -r requirements.txt
	pip3 install -r requirements-dev.txt

dev:
	python3 src/main.py

test: install-dev
	python3 -m pytest tests/ -v

test-ci:
	python3 -m pytest tests/ --cov=src --cov-report=xml --cov-report=term --junitxml=test-results.xml

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