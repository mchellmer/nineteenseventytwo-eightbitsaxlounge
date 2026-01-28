# Changelog

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

[2.0.0]: https://github.com/mchellmer/nineteenseventytwo-eightbitsaxlounge/releases/tag/ui-v2.0.0