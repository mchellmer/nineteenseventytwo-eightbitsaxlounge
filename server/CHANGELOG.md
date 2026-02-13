# Changelog
## 2026-02-13
Fresh build of cluster
- ubuntu: 25.10
  - sudo with ansible is now sudo_rs which doesn't support non-interactive become -> set exe to sudo_ws
- kubernetes: 1.35
- various updates of versions to components
- remove ansible vault dependency
- archive unused components

## Init
Initial build of cluster
- ubuntu: 24.4
- kubernetes: 1.32