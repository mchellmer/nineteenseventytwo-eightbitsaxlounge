# Changelog

## [0.0.2] - 2026-02-11

### Added
- Comprehensive Grafana Cloud setup guide (GRAFANA_SETUP.md)
- Health probes for all service layers:
  - UI: HTTP health endpoints (/health, /ready)
  - Data: /health endpoint with CouchDB connectivity check
  - DB: CouchDB built-in /_up endpoint
  - MIDI: Already had /health endpoint
- Kubernetes liveness and readiness probes for all deployments
- Version labels on all pods for tracking deployments in Grafana
- Dashboard configurations for:
  - System Health (pod status, restarts, versions, resource usage)
  - Logs (errors, volume, command tracking)
- LogQL and PromQL query examples
- Alert configurations for critical issues
- Free tier optimization strategies and usage monitoring
- Health check server for UI Python bot

### Documentation
- Log query patterns by service and severity
- Grafana dashboard setup instructions
- Alert configuration guidance
- Free tier limit management
- Vulnerability scanning integration guide

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
