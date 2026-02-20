# Overlay service

The overlay service manages updating the channel broadcast view based on the changing state of the 8-Bit Sax Lounge. It stores views and connects to the broadcasting service in order to update these.

## Implementation
A Node.js service that subscribes to `ui.overlay.*` NATS subjects and forwards events to connected browsers via `socket.io`. The connected browser is OBS which broadcasts the stream. Html stored here defines what broadcast members see.

Usage
- Build & run locally:
  - docker build -t eightbitsaxlounge-overlay:local .
  - docker run -p 3000:3000 eightbitsaxlounge-overlay:local
- Deploy to Kubernetes (ensure `NATS_URL` points to the in-namespace NATS):
  kubectl -n eightbitsaxlounge-dev apply -f overlay/k8s/deployment.yaml

- Ingress (dev/prod)
  - Template available at `overlay/k8s/ingress.yaml.j2`. Follow the same pattern used by `data` and `db` to set `overlay_ingress_host` (sslip.io) and apply the ingress.
  - Example: `kubectl -n eightbitsaxlounge-dev apply -f overlay/k8s/ingress.yaml` (or use your deployment pipeline to render the j2 template)
  - Once available, OBS can use `http(s)://<overlay_ingress_host>/` or point at `/grid.html` for the test grid



- Smoke test locally (assumes `state` NATS is running via `make test`):
  cd overlay && make smoke   # publishes subject `ui.overlay.engine` with example payload

- Podman (recommended for local container tests)
  1. Start NATS (podman):
     cd state && make test-detach
  2. Build overlay image & run it in Podman:
     cd overlay && make podman-build
     make podman-run            # connects to Podman-hosted NATS via host.containers.internal
  3. Publish a smoke event from container:
     make podman-smoke

How it works
- Subscribes to `ui.overlay.*` (e.g. `ui.overlay.engine`) and emits socket.io events where event name = `overlay.<tail>` (e.g. `overlay.engine`).
- Image mapping: `engine` images live in `public/images/engine/<name>.svg` (e.g. `engine/lofi.svg`, `engine/room.svg`).
- Shared numeric/level images live in `public/images/values/<n>.svg` — `time`, `delay`, `control1` and `control2` resolve their `value` into this `values` folder (e.g. `public/images/values/3.svg`).
- Default images (displayed before any event arrives): `engine` → `public/images/engine/room.svg`; `time`, `delay`, `control1`, `control2` → `public/images/values/0.svg`.
- When an image asset is missing the client will display `public/images/error.svg` (guarded to avoid recursive failures). A `placeholder.svg` is still available as a generic demo image. Sample placeholder, error & demo images are present in `public/images/`.
- A minimal `public/index.html` is included as a demo overlay (browser source for OBS).

Next steps
- Add JWT/NATS credentials and ACLs for production.
- Add ingress and TLS if you want OBS to access the overlay via a public URL.
- Add unit tests and CI/CD build/release workflow.
