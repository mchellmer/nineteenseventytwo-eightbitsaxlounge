# Overlay service

The overlay service manages updating the channel broadcast view based on the changing state of the 8-Bit Sax Lounge. It stores views and connects to the broadcasting service in order to update these.

## Implementation
A Node.js service that subscribes to `ui.overlay.*` NATS subjects and forwards events to connected browsers via `socket.io`. The connected browser is OBS which broadcasts the stream. Html stored here defines what broadcast members see with js scripts that respond based on the event.

Usage
- The Node entrypoint (`src/index.js`) sets up an Express webserver and listens on the provided port.
- The generic `OverlayService` handles logic to create the view, currently the `NatsOverlayService` extends this to setup NATS subscription and response on overlay events. Create further extensions and update index.js to handle differently.

- Build & run locally (expectes running NATS instance):
  - `make podman-build`
  - `make podman-run`
- Deploy to Kubernetes:
  - `make deploy`

- Browser access:
  - kubernetes via Ingress, ClusterIP service port 80->3000:
    - dev: `overlay-dev.<external ip>.sslip.io:80`
    - prod: `overlay.<external ip>.sslip.io:80`
  - local:
    - `http://localhost:3000`

Test
- Integration
  - Ensure nats running at `nats://nats:4222`
  - Build image: `make podman-build`
  - Run webserver in local container: `make podman-run`
  - Via browser: `http://localhost:3000` -> should see expected layout
  - Update values: `make test-integration` -> should see values updated

How it works
- Subscribes to `ui.overlay.*` (e.g. `ui.overlay.engine`) and emits socket.io events where event name = `overlay.<tail>` (e.g. `overlay.engine`).
- Image mapping: `engine` images live in `public/images/engine/<name>.svg` (e.g. `engine/lofi.svg`, `engine/room.svg`).
- Shared numeric/level images live in `public/images/values/<n>.svg` — `time`, `delay`, `control1` and `control2` resolve their `value` into this `values` folder (e.g. `public/images/values/3.svg`).

### Client-side assets

The demo pages reference a common stylesheet (`/css/overlay.css`) and script (`/js/overlay.js`).

The JavaScript file contains the logic that listens for socket.io events matching `overlay.*` and updates the DOM accordingly:

* `setImage` and `setText` helper functions take an element id (or id prefix) and the event payload, pulling a `value` property when available. Images are swapped by setting the `src` attribute; text elements have their `textContent` replaced.
* A mapping function (`getFolderForId`) determines which image subfolder to use (e.g. numeric controls point at `values/`).
* Event handlers for each of the five named fields are wired immediately after the helpers, with a generic `socket.onAny` logger at the end.

## OBS setup 🎛️

Quick steps

1. Add a *Browser Source* in OBS and point the URL to the overlay service:
   - Local dev: `http://localhost:3000/grid.html`
   - In-cluster / production: `https://<overlay_ingress_host>/` (use `/grid.html` for the demo)

2. Recommended Browser Source properties:
   - **Width:** 1920, **Height:** 1080
   - **FPS:** 30
   - **Shutdown source when not visible:** **UNSET** (recommended to keep socket connection)
   - **Refresh browser when scene becomes active:** optional (useful after scene switches)

Troubleshooting

- If the overlay is blank in OBS but works in a normal browser: open `http://localhost:3000/grid.html` in Chrome/Firefox and check devtools for console/network errors.
- Confirm the overlay server is running (logs show `overlay: listening on 3000`) and that NATS is reachable (server logs show `emit event overlay.*`).
- If OBS fails to load remote (HTTPS) overlay, ensure the ingress has valid TLS and the URL is reachable from the OBS host.

Next steps
- Add JWT/NATS credentials and ACLs for production.
- Add ingress and TLS if you want OBS to access the overlay via a public URL.
- Add unit tests and CI/CD build/release workflow.
