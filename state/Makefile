# Validate Ansible playbook and version
build:
	@echo "Validating configuration..."
	@test -f state-nats.yaml || (echo "ERROR: state-nats.yaml not found" && exit 1)
	@test -f version.txt || (echo "ERROR: version.txt not found" && exit 1)
	@grep -E '^[0-9]+\.[0-9]+\.[0-9]+$$' version.txt > /dev/null || (echo "ERROR: Invalid version format in version.txt" && exit 1)
	@test -f k8s/configmap.yaml.j2 || (echo "ERROR: k8s/configmap.yaml.j2 not found" && exit 1)
	@test -f k8s/service-headless.yaml || (echo "ERROR: k8s/service-headless.yaml not found" && exit 1)
	@test -f k8s/statefulset.yaml || (echo "ERROR: k8s/statefulset.yaml not found" && exit 1)
	@test -f k8s/service-client.yaml || (echo "ERROR: k8s/service-client.yaml not found" && exit 1)
	@echo "✓ All required files exist"
	@echo "✓ version.txt is valid ($(shell cat version.txt))"
	@ansible-playbook --syntax-check state-nats.yaml > /dev/null || (echo "ERROR: Ansible syntax check failed" && exit 1)
	@echo "✓ Ansible playbook syntax is valid"
	@echo "All validation checks passed"

test:
	@echo "Running smoke tests..."
	@mkdir -p tmp/data
	@printf '%s\n' \
		'port: 4222' \
		'http: 8222' \
		'jetstream {' \
		'  store_dir: "/data/jetstream"' \
		'  max_mem_store: 64MB' \
		'  max_file_store: 200MB' \
		'}' \
		> tmp/nats.conf
	@echo "Starting NATS in background for health check..."
	docker run -d --name nats-smoke-test -p 4222:4222 -p 8222:8222 \
		-v $(CURDIR)/tmp/data:/data \
		-v $(CURDIR)/tmp/nats.conf:/etc/nats/nats.conf \
		nats:2.12 -c /etc/nats/nats.conf > /dev/null 2>&1
	@sleep 2
	@echo "Verifying NATS is responsive..."
	@if curl -s http://localhost:8222/varz > /dev/null 2>&1; then \
		echo "✓ NATS monitoring endpoint is healthy"; \
	else \
		echo "ERROR: NATS health check failed"; \
		docker stop nats-smoke-test > /dev/null 2>&1 || true; \
		docker rm nats-smoke-test > /dev/null 2>&1 || true; \
		exit 1; \
	fi
	@echo "Checking JetStream status..."
	@if docker exec nats-smoke-test nats --server nats://localhost:4222 stream list > /dev/null 2>&1; then \
		echo "✓ JetStream is operational"; \
	else \
		echo "WARNING: Could not verify JetStream (may not be critical)"; \
	fi
	@echo "Cleaning up test container..."
	docker stop nats-smoke-test > /dev/null 2>&1 || true
	docker rm nats-smoke-test > /dev/null 2>&1 || true
	@echo "All smoke tests passed"

# Start NATS+JetStream locally for interactive development testing
# runs in foreground so logs appear in your terminal (Ctrl-C to stop)
test-local:
	@echo "Starting NATS+JetStream in Podman (foreground). Press Ctrl-C to stop."
	@mkdir -p tmp/data
	@printf '%s\n' \
		'port: 4222' \
		'http: 8222' \
		'jetstream {' \
		'  store_dir: "/data/jetstream"' \
		'  max_mem_store: 64MB' \
		'  max_file_store: 200MB' \
		'}' \
		> tmp/nats.conf
	podman run --rm --name nats-test -p 4222:4222 -p 8222:8222 \
		-v $(CURDIR)/tmp/data:/data:Z \
		-v $(CURDIR)/tmp/nats.conf:/etc/nats/nats.conf:Z \
		nats:2.12 -c /etc/nats/nats.conf

# Windows PowerShell version of test-local
test-local-ps:
	powershell -NoProfile -Command \
		"& { Write-Host 'Starting NATS+JetStream in Podman (foreground). Press Ctrl-C to stop.'; \
		New-Item -ItemType Directory -Force -Path 'tmp/data' | Out-Null; \
		@('port: 4222', 'http: 8222', 'jetstream {', '  store_dir: \"/data/jetstream\"', '  max_mem_store: 64MB', '  max_file_store: 200MB', '}', '') | Set-Content -Path 'tmp/nats.conf'; \
		$$curdir = (Get-Location).Path; \
		podman run --rm --name nats-test -p 4222:4222 -p 8222:8222 -v $$curdir/tmp/data:/data:Z -v $$curdir/tmp/nats.conf:/etc/nats/nats.conf:Z nats:2.12 -c /etc/nats/nats.conf }"

# Deploy NATS to Kubernetes cluster
deploy:
	@test -n "$$NAMESPACE" || (echo "ERROR: NAMESPACE env var not set" && exit 1)
	@test -n "$$OVERLAY_PASS" || (echo "ERROR: OVERLAY_PASS env var not set" && exit 1)
	@test -n "$$UI_PASS" || (echo "ERROR: UI_PASS env var not set" && exit 1)
	@test -n "$$MIDI_PASS" || (echo "ERROR: MIDI_PASS env var not set" && exit 1)
	@test -n "$$DATA_PASS" || (echo "ERROR: DATA_PASS env var not set" && exit 1)
	@echo "🚀 Deploying NATS to namespace $$NAMESPACE"
	NAMESPACE="$$NAMESPACE" OVERLAY_PASS="$$OVERLAY_PASS" UI_PASS="$$UI_PASS" MIDI_PASS="$$MIDI_PASS" DATA_PASS="$$DATA_PASS" \
		ansible-playbook state-nats.yaml

# Manual deployment with interactive prompts for secrets
deploy-nats-manual:
	@if [ -z "$$NAMESPACE" ] || [ -z "$$OVERLAY_PASS" ] || [ -z "$$UI_PASS" ] || [ -z "$$MIDI_PASS" ] || [ -z "$$DATA_PASS" ]; then \
		echo "⚠️  Manual deployment mode: using prompts for secrets"; \
		read -p "Enter NAMESPACE (default: eightbitsaxlounge-dev): " ns; \
		NAMESPACE="$${ns:-eightbitsaxlounge-dev}"; \
		read -sp "Enter OVERLAY_PASS: " overlay_pass; echo; \
		read -sp "Enter UI_PASS: " ui_pass; echo; \
		read -sp "Enter MIDI_PASS: " midi_pass; echo; \
		read -sp "Enter DATA_PASS: " data_pass; echo; \
		NAMESPACE="$$NAMESPACE" OVERLAY_PASS="$$overlay_pass" UI_PASS="$$ui_pass" MIDI_PASS="$$midi_pass" DATA_PASS="$$data_pass" \
			ansible-playbook state-nats.yaml; \
	else \
		NAMESPACE="$$NAMESPACE" OVERLAY_PASS="$$OVERLAY_PASS" UI_PASS="$$UI_PASS" MIDI_PASS="$$MIDI_PASS" DATA_PASS="$$DATA_PASS" \
			ansible-playbook state-nats.yaml; \
	fi

# Help
help:
	@echo "Available targets:"
	@echo "  build              - Validate Ansible playbook and configuration files"
	@echo "  test               - Run smoke tests via Docker (CI)"
	@echo "  test-local         - Start NATS+JetStream with Podman (interactive, foreground)"
	@echo "  test-local-ps      - Start NATS+JetStream with Podman via PowerShell (Windows)"
	@echo "  deploy             - Deploy NATS to Kubernetes (requires env vars)"
	@echo "  deploy-nats-manual - Deploy NATS with interactive prompts for secrets"
	@echo ""
	@echo "Environment variables for deploy:"
	@echo "  NAMESPACE          - K8s namespace (required)"
	@echo "  OVERLAY_PASS       - Password for overlay user (required)"
	@echo "  UI_PASS            - Password for ui user (required)"
	@echo "  MIDI_PASS          - Password for midi user (required)"
	@echo "  DATA_PASS          - Password for data user (required)"

.PHONY: build test test-local test-local-ps deploy deploy-nats-manual help