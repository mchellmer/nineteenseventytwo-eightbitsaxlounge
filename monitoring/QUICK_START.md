# Monitoring Enhancements - Quick Reference

## What Was Changed

### 1. Health Endpoints Added ✅

**UI Layer (Python)**
- Added `health_server.py` with async HTTP server
- Endpoints: `/health` (liveness) and `/ready` (readiness)
- Integrated into `main.py` to run alongside Twitch bot
- Dependency: aiohttp (already in requirements.txt)

**Data Layer (Go)**
- Added `/health` endpoint in `routes.go`
- Added `HealthCheckHandler` in `handlers.go`
- Checks CouchDB connectivity

**DB Layer (CouchDB)**
- Uses built-in `/_up` endpoint

**MIDI Layer**
- Already had `/health` endpoint ✅

### 2. Kubernetes Probes Added ✅

All deployments now have:
- **Liveness Probes**: Restart pod if unhealthy
- **Readiness Probes**: Remove from service if not ready
- **Version Labels**: Track deployments in Grafana

**Configured in:**
- `ui/k8s/deployment.yaml`
- `data/k8s/deployment.yaml`  
- `db/k8s/deployment.yaml`
- `midi/k8s/deployment.yaml` (already had them)

### 3. Documentation Created ✅

**New Files:**
- `monitoring/GRAFANA_SETUP.md` - Comprehensive setup guide
- `monitoring/QUICK_START.md` - This file

**Updated Files:**
- `monitoring/README.md` - Added Grafana setup reference
- `monitoring/CHANGELOG.md` - Documented v0.0.2 changes
- `monitoring/version.txt` - Bumped to 0.0.2

## Next Steps to Complete Setup

### Step 1: Deploy Code Changes

Deploy each layer to get health endpoints active:

```bash
# Update each layer's version and deploy
cd ui && git add . && git commit -m "Add health endpoints" && echo "3.0.8" > version.txt && git push
cd ../data && git add . && git commit -m "Add health endpoint" && echo "0.0.5" > version.txt && git push
cd ../db && git add . && git commit -m "Add health probes" && echo "0.0.4" > version.txt && git push
cd ../monitoring && git add . && git commit -m "Add Grafana setup guide" && git push
```

Or commit everything together and deploy manually.

### Step 2: Verify Health Endpoints

After deployment, test each endpoint:

```bash
# Get pod IPs or port-forward
kubectl port-forward -n eightbitsaxlounge-prod deployment/eightbitsaxlounge-ui 8080:8080
curl http://localhost:8080/health

kubectl port-forward -n eightbitsaxlounge-prod deployment/eightbitsaxlounge-data-api 8081:8080
curl http://localhost:8081/health

kubectl port-forward -n eightbitsaxlounge-prod deployment/eightbitsaxlounge-couchdb 5984:5984
curl http://localhost:5984/_up
```

### Step 3: Check Probe Status

```bash
# View pod status and probe results
kubectl get pods -n eightbitsaxlounge-prod -o wide

# Describe a pod to see probe details
kubectl describe pod -n eightbitsaxlounge-prod <pod-name>

# Watch pod events for probe failures
kubectl get events -n eightbitsaxlounge-prod --watch
```

### Step 4: Set Up Grafana Dashboards

Follow the detailed guide in [GRAFANA_SETUP.md](GRAFANA_SETUP.md):

1. **Explore Logs** (Grafana Cloud → Explore → Loki)
   ```logql
   {namespace="eightbitsaxlounge-prod"}
   ```

2. **Create System Health Dashboard**
   - Pod status
   - Container restarts
   - Probe failures
   - Service versions
   - Resource usage

3. **Create Logs Dashboard**
   - Recent errors
   - Log volume by service
   - Command tracking

4. **Set Up Alerts**
   - Pod crash loops
   - Probe failures  
   - High error rates
   - Service down

### Step 5: Monitor Free Tier Usage

Create a usage dashboard with:

```promql
# Metrics series count (limit: 10,000)
count({__name__=~".+"})

# Container hours per day (limit: 37,900/month)
sum(count_over_time(container_last_seen{namespace=~"eightbitsaxlounge.*"}[24h])) / 60
```

Current usage is safe:
- Metrics: 7.4k/10k (74%) ✅
- Logs: 487MB/50GB (0.9%) ✅  
- Container hours: 6.7k/37.9k (17%) ✅

## Quick Test Commands

### Test Health Endpoints Locally (Before Deploy)

**UI:**
```bash
cd ui
python3 src/main.py &
curl http://localhost:8080/health
```

**Data:**
```bash
cd data
go run . &
curl http://localhost:8080/health
```

### View Logs in Real-Time

```bash
# All services
stern -n eightbitsaxlounge-prod '.*' --since 1m

# Specific service
kubectl logs -n eightbitsaxlounge-prod -l component=ui -f

# UI bot commands
kubectl logs -n eightbitsaxlounge-prod -l component=ui | grep command
```

### Check Probe Failures

```bash
# Pods not ready
kubectl get pods -n eightbitsaxlounge-prod --field-selector=status.phase!=Running

# Pod restart counts
kubectl get pods -n eightbitsaxlounge-prod -o jsonpath='{range .items[*]}{.metadata.name}{"\t"}{.status.containerStatuses[0].restartCount}{"\n"}{end}'
```

## Expected Results

After deployment, you should see in Grafana:

1. **Logs flowing** from all 4 services (ui, midi, data, db)
2. **Health metrics** showing probe success/failure
3. **Version labels** on pods in your queries
4. **INFO level logs** with command execution details
5. **Resource usage** graphs for CPU and memory

## Troubleshooting

**Probe is failing:**
```bash
# Check why a probe is failing
kubectl describe pod <pod-name> -n eightbitsaxlounge-prod
kubectl logs <pod-name> -n eightbitsaxlounge-prod
```

**Health endpoint not responding:**
```bash
# Exec into pod and test locally
kubectl exec -it <pod-name> -n eightbitsaxlounge-prod -- sh
wget -O- http://localhost:8080/health
```

**No logs in Grafana:**
```bash
# Check Alloy log collectors
kubectl get pods -n monitoring -l app.kubernetes.io/name=alloy
kubectl logs -n monitoring -l app.kubernetes.io/component=alloy-logs
```

**Version labels not showing:**
- Ensure VERSION env var is set during deployment
- Check pod labels: `kubectl get pod <pod-name> -o yaml | grep version`

## Reference

For complete details, see:
- [GRAFANA_SETUP.md](GRAFANA_SETUP.md) - Full configuration guide
- [README.md](README.md) - Monitoring layer overview
- [CHANGELOG.md](CHANGELOG.md) - Version history
