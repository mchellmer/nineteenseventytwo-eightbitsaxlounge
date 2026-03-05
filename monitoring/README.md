# Monitoring Layer for EightBitSaxLounge

Kubernetes monitoring configuration using Grafana Cloud and Alloy agents for metrics, logs, traces, and unified correlation ID tracking across all layers.

## Overview

This layer deploys comprehensive monitoring infrastructure to the Kubernetes cluster:
- **Grafana k8s-monitoring** - Managed monitoring stack with remote configuration
- **Alloy Agents** - Distributed collectors for metrics, logs, and traces
- **OpenCost** - Kubernetes cost monitoring
- **Kepler** - Energy efficiency monitoring
- **Unified log format** - All layers now log with correlationID at the end for end-to-end tracing

## Components

### Cluster Metrics
- Node and pod metrics collection
- OpenCost integration for cost tracking
- Kepler for power consumption monitoring
- **Health Probes**: All services now have liveness and readiness probes configured

### Cluster Events
- Kubernetes event collection and forwarding

### Pod Logs
- Application log collection from all pods
- Structured log forwarding to Grafana Loki
- **Unified log format**: `[timestamp] [Information] [layer] message correlationID=<id>`
- **INFO-level logging** enabled across all services for detailed monitoring

### Application Observability
- OTLP receiver for traces (gRPC port 4317, HTTP port 4318)
- Zipkin receiver (port 9411)
- Support for distributed tracing
- **Version labels** on all pods for deployment tracking
- **Correlation ID propagation**: UI → MIDI → Data → DB for full request tracing

### Alloy Agents
- **alloy-metrics** - Metrics collection with remote configuration
- **alloy-singleton** - Singleton metrics like cluster events
- **alloy-logs** - DaemonSet for pod log collection
- **alloy-receiver** - OTLP and Zipkin trace receivers

## Prerequisites

- Ansible installed on control host
- Access to Grafana Cloud account
- Helm v4.1.1+ on target cluster
- Kubernetes cluster running

## Configuration

Edit the playbook variables:
- `grafana_cluster` - Cluster identifier (default: "nineteenseventytwo")
- `helm_version` - Helm version to ensure is installed (default: "v4.1.1")
- `grafana_password` - Access policy token (stored in Ansible Vault)

## Deployment

Run the Ansible playbook from the server directory:

```bash
cd ../server
ansible-playbook monitoring/k8s-monitoring.yaml --ask-vault-pass
```

The playbook will:
1. Check and upgrade Helm if needed
2. Add Grafana Helm repository
3. Install Alloy CRDs
4. Deploy the k8s-monitoring stack with all configurations

## Grafana Cloud Destinations

The monitoring stack sends data to three Grafana Cloud endpoints:
- **Prometheus** - Metrics at prometheus-prod-55-prod-gb-south-1.grafana.net
- **Loki** - Logs at logs-prod-035.grafana.net
- **OTLP Gateway** - Traces, metrics, and logs at otlp-gateway-prod-gb-south-1.grafana.net

## Fleet Management

All Alloy agents are configured with remote fleet management for centralized configuration updates without redeployment.

## Maintenance

### Updating Access Tokens
Access tokens are stored in Ansible Vault. To update:
```bash
ansible-vault edit ../server/group_vars/all/vault.yaml
```

### Version Updates
Update the `helm_version` variable in the playbook to trigger Helm upgrades on next deployment.

### Monitoring the Monitoring
Check Alloy agent status:
```bash
kubectl get pods -n monitoring -l app.kubernetes.io/name=alloy
```
