deploy-nats:
	ansible-playbook state-nats.yaml

apply-k8s:
	kubectl apply -f k8s/

# Run a disposable NATS+JetStream server locally using Podman for quick testing.
# - runs in foreground so logs appear in your terminal (Ctrl-C to stop)
# - creates ./tmp/{nats.conf,data} under this folder for config + storage
test:
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

test-ps:
	powershell -NoProfile -Command \
		"& { Write-Host 'Starting NATS+JetStream in Podman (foreground). Press Ctrl-C to stop.'; \
		New-Item -ItemType Directory -Force -Path 'tmp/data' | Out-Null; \
		@('port: 4222', 'http: 8222', 'jetstream {', '  store_dir: \"/data/jetstream\"', '  max_mem_store: 64MB', '  max_file_store: 200MB', '}', '') | Set-Content -Path 'tmp/nats.conf'; \
		$$curdir = (Get-Location).Path; \
		podman run --rm --name nats-test -p 4222:4222 -p 8222:8222 -v $$curdir/tmp/data:/data:Z -v $$curdir/tmp/nats.conf:/etc/nats/nats.conf:Z nats:2.12 -c /etc/nats/nats.conf }"

# Run NATS in detached/background mode (useful for CI or quick smoke tests)
test-detach:
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
	podman run -d --name nats-test -p 4222:4222 -p 8222:8222 \
		-v $(CURDIR)/tmp/data:/data:Z \
		-v $(CURDIR)/tmp/nats.conf:/etc/nats/nats.conf:Z \
		nats:2.12 -c /etc/nats/nats.conf
	@echo "NATS started in background (podman). To stop: podman stop nats-test"
