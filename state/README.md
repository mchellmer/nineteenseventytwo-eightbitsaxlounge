# State layer

Manage state across the eightbitsaxlounge via message broker

Purpose
- Provide a dedicated, stateful message broker via NATS for microservice events.
- Persistent storage via JetStream

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
