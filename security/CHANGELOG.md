# Changelog

## [0.1.0] - 2026-02-14

### Added
- Initial security scanner component: Trivy-based scanner image, bundled `run-scan.sh`, PVC for Trivy DB cache, and a templated Kubernetes `CronJob`.
- Release pipeline integration (`.github/workflows/security-release.yaml`) and Ansible deploy playbook (`security-deploy.yaml`).

