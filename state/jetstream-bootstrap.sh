#!/bin/sh
# JetStream stream bootstrap
# Runs as a postStart lifecycle hook to create domain streams after NATS starts.
set -e

NATS_URL="${NATS_URL:-nats://127.0.0.1:4222}"
MAX_RETRIES=30
RETRY_DELAY=2

echo "Waiting for NATS server to be ready at $NATS_URL ..."
for attempt in $(seq 1 $MAX_RETRIES); do
  if nats server info -s "$NATS_URL" >/dev/null 2>&1; then
    echo "NATS server is ready"
    break
  fi
  if [ $attempt -eq $MAX_RETRIES ]; then
    echo "ERROR: Failed to connect to NATS after $MAX_RETRIES attempts"
    exit 1
  fi
  echo "Attempt $attempt/$MAX_RETRIES — waiting..."
  sleep $RETRY_DELAY
done

create_stream() {
  local name=$1
  local subjects=$2
  local max_msgs=$3
  nats stream add "$name" \
    --subjects "$subjects" \
    --max-msgs "$max_msgs" \
    --storage file \
    --discard old \
    --replicas 1 \
    -s "$NATS_URL" \
    -n 2>/dev/null \
    && echo "✓ Stream $name created" \
    || echo "ℹ  Stream $name already exists — skipping"
}

echo "Creating JetStream streams..."

create_stream OVERLAY_UPDATES "overlay.>" 1000
create_stream UI_CONTROLS "ui.>" 500
create_stream MIDI_STATE "midi.>" 200
create_stream DATA_API "data.>" 500

echo "JetStream bootstrap complete"
nats stream list -s "$NATS_URL"
