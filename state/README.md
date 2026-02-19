# State layer — NATS / JetStream

This folder contains the Kubernetes + Ansible manifests used to run the state/eventing tier (NATS JetStream) for EightBitSaxLounge.

Purpose
- Provide a dedicated, stateful message broker (NATS + JetStream) for microservice events.
- Persistent JetStream storage is backed by Longhorn PVCs (per-pod PVC via StatefulSet).

Deploy (example)
- Apply directly with kubectl (namespace must exist):
  kubectl -n eightbitsaxlounge-dev apply -f state/k8s/nats.yaml

- Or use the included Ansible playbook:
  NAMESPACE=eightbitsaxlounge-dev ansible-playbook state/state-nats.yaml

Configuration highlights
- `volumeClaimTemplates` uses `storageClassName: longhorn` by default.
- DNS within namespace: `nats:4222` (set `NATS_URL=nats://nats:4222` for services).
- For production HA/scale run 3+ replicas and configure NATS clustering/routes.

Testing
- Quick smoke test using `nats-box` (k8s):
  kubectl -n eightbitsaxlounge-dev run --rm -i --tty nats-test --image=natsio/nats-box --restart=Never -- nats sub test & nats pub test "hello"

- Local Podman test (fast, no k8s):
  1. cd state
  2. make test            # runs NATS+JetStream in foreground (Ctrl-C to stop)
     or
     make test-detach    # runs NATS in background (podman stop nats-test)
  3. Verify via: curl http://localhost:8222/  or use the `nats-box` image to pub/sub to `ui.overlay.*`

Next steps
- Create NATS accounts / credentials and ACLs for each environment.
- Create JetStream streams for logical areas (e.g. `UI_OVERLAY` for UI overlay updates).
- Scaffold client libraries or services to publish/subscribe (I can scaffold a Node overlay that subscribes to `ui.overlay.*`).
