# CouchDB Deployment Resources

This folder contains resources for building, deploying, and managing a CouchDB instance in a Kubernetes environment. It includes a Dockerfile for creating a CouchDB container image, Kubernetes manifests for deployment, and a GitHub Actions workflow and Ansible playbook for automating the build and deployment process with versioned images and host-based ingress.

## Overview

The resources in this folder are designed to:
1. Build a CouchDB Docker image.
2. Deploy the CouchDB container to a Kubernetes cluster using the provided manifests.
3. Automate the build and deployment process using a GitHub Actions workflow.

## Repository Structure

- **`Dockerfile`**: Defines the steps to build a CouchDB container image.
- **`k8s/`**: Contains Kubernetes manifests for deploying CouchDB:
  - `deployment.yaml`: Defines the CouchDB deployment with persistent volume.
  - `persistentvolumeclaim.yaml`: 10GB persistent volume claim for data persistence.
  - `service.yaml`: Exposes the CouchDB deployment as a service within the cluster.
  - `ingress.yaml.j2`: Templated Ingress (host-based) to expose CouchDB/Fauxton externally.
- **`.github/workflows/db-release.yaml`**: A GitHub Actions workflow to automate the build, push, and deployment process.
  - Triggers on updates to `version.txt`
  - Builds and pushes `ghcr.io/<owner>/eightbitsaxlounge-couchdb:<version>` and `:latest`
  - Merging to main deploys to namespace `eightbitsaxlounge-prod`; other branches to `eightbitsaxlounge-dev`
- **`db-couchdb.yaml`**: An Ansible playbook for deployment db components to cluster
- **`Makefile`**: Makefile defining steps to deploy db components
- **`CHANGELOG.md`**: Version history and changes

## Data Persistence

CouchDB data is stored in a PersistentVolumeClaim (10GB) mounted at `/opt/couchdb/data`. This ensures data persists across:
- Pod restarts
- Deployment updates
- Cluster shutdowns

**Note**: When upgrading from a deployment without persistence, existing data will not be automatically migrated. You can either:
1. Re-initialize databases using the MIDI data-init and data-upload workflows
2. Manually backup and restore data before deploying the PVC update

## Prerequisites

Before using these resources, ensure the following:
- Docker is installed and configured on your local machine or runner.
- Kubernetes is installed and configured, with access to the target cluster.
- The following secrets are configured in the GitHub repository settings:
  - `GITHUB_TOKEN`: Automatically provided by GitHub for authenticating with the GitHub Container Registry (GHCR).

## Accessing CouchDB (Fauxton UI)

The Ingress exposes CouchDB via host-based routing using the ingress-nginx external IP (MetalLB) and sslip.io.

- Dev: `http://db-dev.<INGRESS_IP>.sslip.io/_utils/`
- Prod: `http://db.<INGRESS_IP>.sslip.io/_utils/`

Authentication uses the admin credentials configured in the Deployment (password from Secret `secret-db-couchdb`).

To find the IP:
```bash
kubectl -n ingress-nginx get svc ingress-nginx-controller -o jsonpath='{.status.loadBalancer.ingress[0].ip}{"\n"}'
```

If DNS resolution is unavailable, test with a Host header:
```bash
curl -i -H "Host: db-dev.<IP>.sslip.io" http://<IP>/_utils/
```

## CI/CD and Versioning

- Image version comes from `db/version.txt`.
- CI builds and pushes both `:latest` and `:<version>` to GHCR.
- The Ansible playbook patches the Deployment to the exact `:<version>` tag, annotates the change cause, waits for rollout, and prints the running image.

## Test
```
# Check pod service and endpoint
kubectl -n eightbitsaxlounge-dev get pods -o wide
kubectl -n eightbitsaxlounge-dev get svc db-service
kubectl -n eightbitsaxlounge-dev get endpoints db-service

# Public welcome doc (no auth)
curl -s http://<cluster ip>:5984/

# List DBs (requires admin)
curl -s -u admin:$DB_COUCHDB_PASSWORD http://<cluster ip>:5984/_all_dbs
```

## Monitoring & Logging
- Unified log format: `[timestamp] [Information] [db] message correlationID=<id>`
- Correlation ID is propagated from Data layer for end-to-end tracing in Grafana
- Version labels on pods for deployment tracking