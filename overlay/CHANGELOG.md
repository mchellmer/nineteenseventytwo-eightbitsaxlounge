# Changelog

## [1.0.4] - 2026-03-11

### Added
- Dynamic font size scaling for engine panel: shorter scale applied for names ≥6, ≥7, ≥8 characters to prevent overflow
- Socket.IO event handlers for MIDI-published overlay subjects: `overlay.predelay` (maps to delay panel), `overlay.control1` (maps to dial1 panel), `overlay.control2` (maps to dial2 panel)
- `overlay.player` socket event handler for player name panel updates
- `chat` panel coordinates added to `TwitchConsole.json` with StreamElements widget alignment data (1920×1080 canvas, position and size derived from background image scale)

### Changed
- Panel rendering switched from image-based (`setImage`/`setText`) to text-based (`setPanelText`) for all panels
- `adjustPanels` now re-runs on every `setPanelText` call and on window resize and DOMContentLoaded for consistent sizing
- Pure functions (`adjustPanels`, `setPanelText`) exported for Jest unit testing; `init()` only auto-calls in browser context

## [0.0.2] - 2026-03-07

### Added
- Node.js-based overlay service for OBS browser source integration
- NATS event subscription to `overlay.*` subjects for real-time broadcast updates
- Socket.IO push events to connected browsers for state synchronization
- Express HTTP server with static file serving from `public/` directory
- Extensible OverlayService architecture with NatsOverlayService implementation
- Docker containerization with multi-stage builds matching Alpine patterns
- Makefile build/test/deploy automation with Docker-based test execution
- GitHub Actions CI/CD workflow using release-template.yaml
- Ansible playbook for Kubernetes deployment with ingress routing
- Ingress-based browser access (dev: overlay-dev.*, prod: overlay.*)
- NATS authentication with overlay user ACL (publish/subscribe to overlay.>)
- In-cluster credential management via state layer secret injection
- Comprehensive testing: Jest unit tests + image asset verification
- Client-side JavaScript for socket.io event handling and DOM updates
- SVG-based image assets for engine types and numeric values
- Integration test support for local development workflows

### Configuration
- Port: 3000 (configurable via PORT environment variable)
- NATS connectivity: Configurable via NATS_URL, NATS_USER, NATS_PASS environment variables
- Service mesh: ClusterIP service on port 80 (maps to 3000)
- Ingress: NGINX controller with automatic host routing based on namespace
