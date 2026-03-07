# Changelog

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
