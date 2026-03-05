# Changelog

## [1.0.3]

### Changed
- Unified log format across all layers: logs now include correlationID at the end for easier tracing in Grafana.
- INFO-level logging enabled for all API endpoints.
- Health check endpoints now excluded from correlation ID logging.
- Version labels added to all pods for deployment tracking.
- Improved Prometheus/Grafana dashboard queries for pod, node, and deployment health.

### Fixed
- Correlation ID propagation from MIDI to Data layer.
- LogQL queries and dashboard transformations for clearer monitoring tables.

## [Previous]
- See earlier entries for initial API, CouchDB integration, and Kubernetes deployment.
