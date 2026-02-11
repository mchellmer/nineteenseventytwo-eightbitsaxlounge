# Grafana Cloud Configuration Guide

This guide helps you configure Grafana Cloud to monitor your EightBitSaxLounge cluster effectively while staying within free tier limits.

## Table of Contents
1. [Log Queries](#log-queries)
2. [Dashboard Setup](#dashboard-setup)
3. [Useful Queries](#useful-queries)
4. [Alerts](#alerts)
5. [Free Tier Management](#free-tier-management)

## Log Queries

### View Logs by Service

**UI Layer Logs:**
```logql
{namespace="eightbitsaxlounge-prod", component="ui"} |= ``
```

**MIDI Layer Logs:**
```logql
{namespace="eightbitsaxlounge-prod", component="midi"} |= ``
```

**Data Layer Logs:**
```logql
{namespace="eightbitsaxlounge-prod", component="data"} |= ``
```

**DB Layer Logs:**
```logql
{namespace="eightbitsaxlounge-prod", component="db"} |= ``
```

### Filter by Log Level

**INFO level logs:**
```logql
{namespace="eightbitsaxlounge-prod"} |= "INFO"
```

**ERROR logs across all services:**
```logql
{namespace="eightbitsaxlounge-prod"} |= "ERROR"
```

**WARNING logs:**
```logql
{namespace="eightbitsaxlounge-prod"} |~ "WARN|WARNING"
```

### Specific Log Patterns

**MIDI commands executed:**
```logql
{namespace="eightbitsaxlounge-prod", component="ui"} |= "command" |= "INFO"
```

**Health check failures:**
```logql
{namespace="eightbitsaxlounge-prod"} |~ "unhealthy|not_ready|health.*fail"
```

**Database connection issues:**
```logql
{namespace="eightbitsaxlounge-prod"} |= "database" |~ "error|fail|timeout"
```

## Dashboard Setup

### Create a System Health Dashboard

1. Go to Grafana Cloud → Dashboards → New Dashboard
2. Name it "EightBitSaxLounge - System Health"
3. Add the following panels:

#### Panel 1: Pod Status
**Query Type:** Prometheus  
**Query:**
```promql
kube_pod_status_phase{namespace=~"eightbitsaxlounge.*"}
```
**Visualization:** Stat
**Description:** Shows running status of all pods

#### Panel 2: Container Restarts
**Query Type:** Prometheus  
**Query:**
```promql
rate(kube_pod_container_status_restarts_total{namespace=~"eightbitsaxlounge.*"}[5m])
```
**Visualization:** Time series
**Description:** Track pod restart rates

#### Panel 3: Probe Failures
**Query Type:** Prometheus  
**Query:**
```promql
sum by (pod, probe) (
  kube_pod_container_status_ready{namespace=~"eightbitsaxlounge.*"} == 0
)
```
**Visualization:** Table
**Description:** Shows which pods are failing readiness checks

#### Panel 4: Service Versions
**Query Type:** Prometheus  
**Query:**
```promql
kube_pod_labels{namespace=~"eightbitsaxlounge.*", label_version!=""}
```
**Visualization:** Table
**Transform:** Extract `label_version` field
**Description:** Shows deployed version of each service

#### Panel 5: Memory Usage by Service
**Query Type:** Prometheus  
**Query:**
```promql
sum by (component) (
  container_memory_usage_bytes{namespace=~"eightbitsaxlounge.*", container!=""}
)
```
**Visualization:** Time series
**Description:** Memory consumption per component

#### Panel 6: CPU Usage by Service
**Query Type:** Prometheus  
**Query:**
```promql
sum by (component) (
  rate(container_cpu_usage_seconds_total{namespace=~"eightbitsaxlounge.*", container!=""}[5m])
)
```
**Visualization:** Time series
**Description:** CPU usage per component

### Create a Logs Dashboard

1. Create another dashboard named "EightBitSaxLounge - Logs"
2. Add these panels:

#### Panel 1: Recent Errors
**Query Type:** Loki  
**Query:**
```logql
{namespace=~"eightbitsaxlounge.*"} |= "ERROR"
```
**Visualization:** Logs
**Time range:** Last 15 minutes

#### Panel 2: Log Volume by Service
**Query Type:** Loki  
**Query:**
```logql
sum by (component) (
  count_over_time({namespace=~"eightbitsaxlounge.*"}[1m])
)
```
**Visualization:** Time series
**Description:** Shows log volume to monitor free tier usage

#### Panel 3: Command Execution (UI)
**Query Type:** Loki  
**Query:**
```logql
{namespace=~"eightbitsaxlounge.*", component="ui"} |= "command" |= "INFO"
```
**Visualization:** Logs
**Description:** Track viewer commands

## Useful Queries

### Health Status Summary
```promql
sum by (namespace, component) (
  up{namespace=~"eightbitsaxlounge.*"}
)
```

### Logs Ingestion Rate (monitor free tier)
```logql
sum(
  rate({namespace=~"eightbitsaxlounge.*"}[1m])
) * 60
```
**Result in logs per minute** - multiply by 1440 to get daily estimate

### Container Count
```promql
count by (namespace) (
  kube_pod_container_info{namespace=~"eightbitsaxlounge.*"}
)
```

### Failed Deployments
```promql
kube_deployment_status_replicas_available{namespace=~"eightbitsaxlounge.*"}
  != 
kube_deployment_spec_replicas{namespace=~"eightbitsaxlounge.*"}
```

## Alerts

### Critical Alerts (Set these up)

#### 1. Pod Crash Loop
**Condition:**
```promql
rate(kube_pod_container_status_restarts_total{namespace=~"eightbitsaxlounge.*"}[15m]) > 0
```
**Alert when:** > 0 for 5 minutes
**Severity:** Critical

#### 2. Probe Failures
**Condition:**
```promql
kube_pod_container_status_ready{namespace=~"eightbitsaxlounge.*"} == 0
```
**Alert when:** true for 2 minutes
**Severity:** Warning

#### 3. High Error Rate
**Condition (Loki):**
```logql
sum(
  rate({namespace=~"eightbitsaxlounge.*"} |= "ERROR" [5m])
) > 10
```
**Alert when:** > 10 errors/sec
**Severity:** Warning

#### 4. Service Down
**Condition:**
```promql
up{namespace=~"eightbitsaxlounge.*"} == 0
```
**Alert when:** true for 1 minute
**Severity:** Critical

## Free Tier Management

### Current Limits (Your Account)
- **Metrics**: 10,000 series (currently 7,400 - 74%)
- **Logs**: 50 GB/month (currently 487 MB - 0.9%)
- **Traces**: 50 GB/month (currently 0 - 0%)
- **Host hours**: 2,200/month (currently 360 - 16%)
- **Container hours**: 37,900/month (currently 6,700 - 17%)

### Optimization Strategies

#### 1. Reduce Metric Cardinality

Disable unnecessary metrics in `k8s-monitoring.yaml`:

```yaml
clusterMetrics:
  enabled: true
  # Disable if cost monitoring isn't needed
  opencost:
    enabled: false  # Saves ~500 series
  
  # Disable if power monitoring isn't needed
  kepler:
    enabled: false  # Saves ~300 series
```

#### 2. Log Sampling

For high-volume debug logs, add sampling in your monitoring config:

```yaml
podLogs:
  enabled: true
  # Sample logs to reduce volume
  extraConfig: |
    drop {
      source_labels = ["level"]
      regex = "DEBUG"
      action = "drop"
    }
```

#### 3. Retention Settings

Logs are retained based on your plan. Free tier typically keeps:
- Metrics: 14 days
- Logs: 14 days  
- Traces: 14 days

#### 4. Monitor Your Usage

Create a **Usage Dashboard** to track your limits:

**Metrics Series Count:**
```promql
count({__name__=~".+"})
```

**Container Hours per Day:**
```promql
sum(
  count_over_time(container_last_seen{namespace=~"eightbitsaxlounge.*"}[24h])
) / 60
```

### Safe Configuration for Free Tier

Your current setup is very safe for free tier with:
- 4 main services (ui, midi, data, db)
- ~4-6 pods total
- Low log volume
- Standard metrics collection

**Recommendations:**
1. ✅ Keep OpenCost and Kepler enabled (you're at 74% of metrics limit)
2. ✅ Enable INFO level logging for all services (plenty of log headroom)
3. ✅ Add health probes (minimal metric impact)
4. ✅ Enable application observability (you have 50GB traces unused)
5. ⚠️ Monitor if you add more services/replicas

### Setting Up Alerts for Free Tier

Alert when approaching limits:

**Approaching Metrics Limit:**
```promql
count({__name__=~".+"}) > 9000
```

**Log Ingestion Rate High:**
```logql
sum(
  rate({namespace=~"eightbitsaxlounge.*"}[1h])
) * 3600 * 24 > 1000000000  # 1GB/day
```

## Next Steps

1. ✅ Deploy updated services with health probes
2. ✅ Verify logs are flowing in Grafana Cloud → Explore → Loki
3. ✅ Create the System Health dashboard
4. ✅ Create the Logs dashboard  
5. ✅ Set up critical alerts
6. ✅ Add usage monitoring

## Vulnerability Scanning

Grafana Cloud doesn't include built-in vulnerability scanning, but you can:

1. **Enable in CI/CD Pipeline**
   - Already configured with Trivy in your release workflow
   - Results can be viewed in GitHub Security tab

2. **Kubescape Integration** (Optional)
   ```bash
   helm install kubescape kubescape/kubescape-operator
   ```
   Then export results to Prometheus/Grafana

3. **View in GitHub**
   - Security → Code scanning alerts
   - Security → Dependabot alerts

4. **Manual Scans**
   ```bash
   trivy image ghcr.io/mchellmer/eightbitsaxlounge-ui:latest
   ```

For cost-effective vulnerability monitoring, stick with GitHub's free security features and your existing CI/CD Trivy scans.
