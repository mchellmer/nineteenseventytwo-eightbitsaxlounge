# Changelog

## [2.0.8]

### Added
- Data initialization endpoint (`POST /api/Midi/InitializeDataModel`) to create CouchDB databases and views
- Data upload endpoints for effects (`POST /api/Midi/UploadEffects`) and devices (`POST /api/Midi/UploadDevice/{deviceName}`)
- Handler pattern architecture for endpoint logic separation
- Device configuration data model matching CouchDB schema (devices, effects, selectors)
- Configuration files: `appsettings.Effects.json` and `appsettings.Devices.VentrisDualReverb.json`
- GitHub Actions workflows for data initialization and upload
- Support for device-specific effect mappings with MIDI implementation details
- DeviceName field in effect device settings for multi-device support

### Changed
- Restructured data model to align with CouchDB document structure
- Updated effects configuration to include device-specific settings
- Refactored endpoint handlers into dedicated handler classes implementing `IEndpointHandler<TRequest, TResponse>`
- Enhanced data upload workflows with support for effects and devices upload types

## [1.0.7] - 2026-01-23

### Added
- Device proxy service for Kubernetes deployment
- HTTP-based MIDI device proxy with bypass key authentication
- Conditional service registration based on `MidiDeviceService:Url` configuration
- Support for both direct device access (Windows PC) and proxy mode (K8s container)
- Kubernetes deployment manifests (deployment, service, configmap, secret)
- Ansible-based deployment automation for K8s cluster
- Environment-specific device service URLs (dev: port 5000, prod: port 5001)
- GitHub Container Registry integration
- Consolidated CI/CD workflow for both PC and K8s deployments
- Bypass key authentication for internal K8s → Windows PC communication
- JWT token authentication requirement for external API requests

### Changed
- Split deployment architecture: K8s service for data endpoints + proxy to Windows PC for device control
- Updated endpoint handlers to support proxy pattern
- Enhanced health checks for containerized deployment
- Dockerfile updated to use .NET 8.0 (matching project target framework)
- Authentication middleware order to support bypass key only on Windows PC service
- K8s service requires JWT authentication, sends bypass key when proxying to Windows PC

### Fixed
- .NET runtime version mismatch in Docker container
- Missing token expiry configuration in K8s deployment
- Authentication flow for proxy requests
- Middleware execution order for bypass key validation

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

[1.0.7]: https://github.com/mchellmer/nineteenseventytwo-eightbitsaxlounge/releases/tag/midi-api-v1.0.0
[0.0.7]: https://github.com/mchellmer/nineteenseventytwo-eightbitsaxlounge/releases/tag/midi-api-v0.0.7
