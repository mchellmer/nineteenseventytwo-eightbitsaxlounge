# Changelog
 
## [5.0.8] - 2026-03-08

### Added
- PersistentVolumeClaim for token database storage (`/app/tokens/tokens.db`) on Longhorn
- Persistent token storage survives pod restarts without re-authorization
- Comprehensive Twitch setup documentation with OAuth authorization flow
- Error event handler (`event_error`) to surface silent twitchio errors
- Component initialization logging for debugging

### Changed
- renamed layer from 'ui' to 'chat'
- Migrated from ephemeral container storage to persistent volume-backed tokens
- Fixed user ID type handling: convert all user IDs to strings for EventSub subscriptions (int/string consistency)
- Updated Dockerfile to create app user with explicit UID 1000 for permission compatibility
- Added pod-level `fsGroup: 1000` security context for PVC ownership handling
- Improved OAuth subscription error handling: 409 conflicts logged at INFO level (already exists)
- Updated Ansible playbook to deploy PersistentVolumeClaim during initial setup
- Deployment now uses `longhorn` storage class instead of local-path

### Fixed
- Database file creation permissions issue in mounted PVC
- User ID type mismatches causing EventSub subscription failures
- Bot account channel exclusion logic (skip self-subscription)
- PVC mount permission errors by using fsGroup in pod security context

## [4.0.28] - 2026-02-17

### Added
- Twitchio v3 with automated token management
- python 3.14 slim base image
- upgrade dependencies to latest

## [3.0.10] - 2026-02-12

### Changed
- Unified log format: `[timestamp] [Information] [chat] message correlationID=<id>`
- Correlation ID propagation to MIDI and Data layers
- Health check log exclusion for correlation ID tracking
- Grafana dashboard improvements for CHAT logs and health
- Version labels on pods for deployment tracking

## [3.0.7] - 2026-02-08

### Added
- Case-insensitive command support - commands now work regardless of capitalization (e.g., !engine, !Engine, !ENGINE)
- New value-based commands with 0-10 to MIDI 0-127 scaling:
  - `!time <0-10>` - Set reverb decay time
  - `!predelay <0-10>` - Set reverb pre-delay
  - `!control1 <0-10>` - Set custom control 1
  - `!control2 <0-10>` - Set custom control 2
- Multi-message help command for better Twitch chat display
- INFO-level logging for command execution

### Changed
- Help command now displays messages in multiple parts to avoid rate limiting
- Help command dynamically shows available reverb engines from settings
- Improved value display formatting - whole numbers display without decimal points (e.g., "5" instead of "5.0")
- CommandHandler interface now supports returning either string or list of strings

### Fixed
- Twitch message rate limiting - added 1.5s delays between multi-line messages
- Help command tests updated to handle list return type

[3.0.7]: https://github.com/mchellmer/nineteenseventytwo-eightbitsaxlounge/releases/tag/chat-v3.0.7

## [2.0.1] - 2026-01-26

### Fixed
- Resolved lint issues

## [2.0.0] - 2026-01-26

### Changed
- Updated MIDI client to use single Kubernetes service endpoint instead of dual device/data URLs
- Simplified MidiClient constructor to use single `base_url` parameter
- Removed endpoint routing logic for device vs data requests
- All MIDI API requests now route through `eightbitsaxlounge-midi-service:8080`
- Updated GitHub Actions workflow to remove external MIDI_DEVICE_URL secret dependency
- Updated Ansible deployment playbook to use internal Kubernetes service URL
- Refactored Ansible playbook with comprehensive variables section for better maintainability

### Removed
- `midi_data_url` configuration setting (no longer needed)
- `DEVICE_ENDPOINTS` constant and `_get_base_url()` method from MidiClient
- External MIDI service URL dependency from CI/CD pipeline

### Technical Details
- MIDI client now communicates exclusively with Kubernetes ClusterIP service
- Internal cluster communication replaces external endpoint access
- Improved deployment consistency and security through service mesh communication

[2.0.0]: https://github.com/mchellmer/nineteenseventytwo-eightbitsaxlounge/releases/tag/chat-v2.0.0