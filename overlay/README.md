# Overlay service

A small Node.js service that subscribes to `ui.overlay.*` NATS subjects and forwards events to connected browsers via `socket.io`.

Usage
- Build & run locally:
  - docker build -t eightbitsaxlounge-overlay:local .
  - docker run -p 3000:3000 eightbitsaxlounge-overlay:local
- Deploy to Kubernetes (ensure `NATS_URL` points to the in-namespace NATS):
  kubectl -n eightbitsaxlounge-dev apply -f overlay/k8s/deployment.yaml

How it works
- Subscribes to `ui.overlay.*` (e.g. `ui.overlay.engine`) and emits socket.io events where event name = `overlay.<tail>` (e.g. `overlay.engine`).
- A minimal `public/index.html` is included as a demo overlay (browser source for OBS).

Next steps
- Add JWT/NATS credentials and ACLs for production.
- Add ingress and TLS if you want OBS to access the overlay via a public URL.
- Add unit tests and CI/CD build/release workflow.
