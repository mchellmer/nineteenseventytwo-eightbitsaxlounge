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

Testing
- Local Podman test:
  1. cd state
  2. make test            # runs NATS+JetStream in foreground (Ctrl-C to stop)
  3. Verify via: curl http://localhost:8222/
