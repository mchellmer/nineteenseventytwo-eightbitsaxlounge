# Changelog
## [1.0.0] - 2026-01-23

### Added
- Device proxy service for Kubernetes deployment
- HTTP-based MIDI device proxy (`MidiDeviceProxyService`)
- Conditional service registration based on `MidiDeviceService:Url` configuration
- Support for both direct device access (Windows PC) and proxy mode (K8s container)
- Kubernetes deployment manifests (deployment, service, configmap, secret)
- Ansible-based deployment automation for K8s cluster
- Environment-specific device service URLs (dev: port 5000, prod: port 5001)
- GitHub Container Registry integration
- Consolidated CI/CD workflow for both PC and K8s deployments

### Changed
- Split deployment architecture: K8s service for data endpoints + proxy to Windows PC for device control
- Updated endpoint handlers to support proxy pattern
- Enhanced health checks for containerized deployment

## [0.0.7] - 2026-01-18

### Added
- Initial MIDI service implementation
- Direct Windows MIDI device access via WinMM
- JWT authentication with client credentials
- Device control endpoints (SendControlChangeMessage, etc.)
- Data endpoints (placeholder for CouchDB integration)
- NSSM-based Windows service deployment
- Self-contained Windows x64 binary packaging
- GitHub Actions workflow for Windows PC deployment

[1.0.0]: https://github.com/mchellmer/nineteenseventytwo-eightbitsaxlounge/releases/tag/midi-api-v1.0.0
[0.0.7]: https://github.com/mchellmer/nineteenseventytwo-eightbitsaxlounge/releases/tag/midi-api-v0.0.7
