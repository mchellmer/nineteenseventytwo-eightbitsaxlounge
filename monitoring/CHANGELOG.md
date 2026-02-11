# Changelog

## [0.0.1] - 2026-02-11

### Added
- Initial monitoring layer setup
- Grafana k8s-monitoring Helm chart deployment via Ansible
- Multi-destination configuration:
  - Prometheus for metrics
  - Loki for logs
  - OTLP gateway for traces, metrics, and logs
- Alloy agent configurations:
  - alloy-metrics for cluster metrics collection
  - alloy-singleton for singleton metrics
  - alloy-logs for pod log collection (DaemonSet)
  - alloy-receiver for OTLP and Zipkin traces
- Remote fleet management for all Alloy agents
- OpenCost integration for Kubernetes cost monitoring
- Kepler integration for energy efficiency monitoring
- Application observability receivers:
  - OTLP gRPC (port 4317)
  - OTLP HTTP (port 4318)
  - Zipkin (port 9411)
- Automatic Helm v4.1.1 upgrade capability
- Version checking for conditional Helm installation
