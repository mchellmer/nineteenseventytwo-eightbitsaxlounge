docker-build:
	docker build -t eightbitsaxlounge-overlay:local .

docker-run:
	docker run --rm -p 3000:3000 eightbitsaxlounge-overlay:local

deploy:
	kubectl apply -f k8s/deployment.yaml

# Publish a test message to the local NATS (useful when you run `state make test`)
smoke:
	@echo "Publishing sample ui.overlay.engine -> NATS (NATS_URL=$${NATS_URL:-nats://127.0.0.1:4222})"
	node scripts/publish-sample.js

# Podman helpers — run the overlay container locally and connect to a NATS container
podman-build:
	podman build -t eightbitsaxlounge-overlay:local .

# Run overlay (bind to localhost:3000). If NATS is running in Podman and published to host
# use NATS_URL=nats://host.containers.internal:4222 (Podman Desktop DNS mapping).
podman-run:
	podman run --rm -p 3000:3000 \
		-e NATS_URL=$${NATS_URL:-nats://host.containers.internal:4222} \
		eightbitsaxlounge-overlay:local

# Run the smoke publisher from inside a container (connects to host NATS by default)
podman-smoke:
	podman run --rm \
		-e NATS_URL=$${NATS_URL:-nats://host.containers.internal:4222} \
		eightbitsaxlounge-overlay:local npm run smoke
