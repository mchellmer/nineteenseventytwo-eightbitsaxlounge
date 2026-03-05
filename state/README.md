# State layer

Manage state across the eightbitsaxlounge via a message broker

Purpose
- Provide a dedicated, stateful message broker via NATS for microservice events.
- Persistent storage via JetStream

Usage

The state layer runs a single‑node NATS server with JetStream enabled.  To
reach it from other components use the Kubernetes service DNS name
(`eightbitsaxlounge-state-client`).

A centralized credential secret (`state-nats-creds`) is created by the state
playbook.  You may override any of the default user/password pairs by
exporting environment variables.

Credentials are managed centrally via a secret named `state-nats-creds`.
This secret contains the following base64‑encoded keys:

```
overlay_pass
ui_pass
midi_pass
data_pass
```

```sh
# from another pod in the same namespace
export NATS_URL=nats://eightbitsaxlounge-state-client:4222
# or with host/port explicitly
export NATS_URL=nats://eightbitsaxlounge-state-client.default.svc.cluster.local:4222
```

For step‑1 testing with the overlay service you can exercise the
new authorization rules:

```sh
cd state && make test &                # start NATS locally via Podman
export NATS_URL=nats://127.0.0.1:4222
export NATS_USER=overlay
export NATS_PASS=overlaypw
cd ../overlay && make podman-build && make podman-run
cd ../overlay && make smoke          # publish a test message
```

The overlay container should connect and updates will appear at
`http://localhost:3000/grid.html`.  If the credentials are wrong the
NATS connection will be rejected.

Testing
- Local Podman test:
  1. cd state
  2. make test            # runs NATS+JetStream in foreground (Ctrl-C to stop)
  3. Verify via: curl http://localhost:8222/

Next steps
1. **Secure the broker.**
   - Define NATS accounts/users and generate credentials.
   - Write ACL rules so that e.g. the overlay service may publish to `ui.overlay.*` but cannot subscribe to internal streams.
   - For early testing the configmap already contains a simple `overlay:overlaypw` user; overlay components can connect with
     `NATS_USER=overlay NATS_PASS=overlaypw` or by creating a secret containing those values.
   - Consider storing creds in Kubernetes secrets (e.g. `overlay-nats-creds`) and mounting/consuming them via environment
     variables in your deployment manifests.

2. **Structure JetStream.**
   - Create streams for your domain areas (`UI_OVERLAY`, `DATA_API`, etc.).
   - Configure limits/retention policies appropriate for state data versus event logs.
   - Experiment locally using the CLI or the management APIs (`nc.jetstream()` in JS).

3. **Client helpers.**
   - Build or document wrapper functions for common patterns (e.g. `publishOverlayUpdate(id,val)`).
   - The overlay service already acts as a subscriber; similar scaffolding could be added to the data API or any other consumer.

4. **Integration and CI.**
   - Extend the existing `make test` to create streams and verify basic publishes/subscribes.
   - Add Ansible playbook steps to create streams/accounts when deploying to a cluster.

5. **Scale & HA.**
   - Consider running 3+ replicas of the StatefulSet and enabling clustering in `nats.conf` once you need redundancy.
   - Update the headless service and ingress logic accordingly.

Following these steps will turn the lightweight proof‑of‑concept broker into a production‑ready state layer with authentication, structured streams, and automation.