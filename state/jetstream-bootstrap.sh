#!/bin/sh
# JetStream stream bootstrap
# Runs as a postStart lifecycle hook to create domain streams after NATS starts.

NATS_URL="${NATS_URL:-nats://127.0.0.1:4222}"
NATS_USER="${NATS_USER:-system}"
NATS_PASS="${NATS_PASS:-$SYSTEM_PASS}"
MAX_RETRIES=30
RETRY_DELAY=2

# Validate required credentials
if [ -z "$NATS_PASS" ]; then
  echo "ERROR: NATS_PASS or SYSTEM_PASS not set"
  exit 1
fi

echo "Waiting for NATS server to be ready at $NATS_URL ..."
attempt=1
while [ $attempt -le $MAX_RETRIES ]; do
  if nats server info -s "$NATS_URL" -u "$NATS_USER" -p "$NATS_PASS" >/dev/null 2>&1; then
    echo "NATS server is ready"
    break
  fi
  if [ $attempt -eq $MAX_RETRIES ]; then
    echo "ERROR: Failed to connect to NATS after $MAX_RETRIES attempts"
    exit 1
  fi
  echo "Attempt $attempt/$MAX_RETRIES — waiting..."
  sleep $RETRY_DELAY
  attempt=$((attempt + 1))
done

create_stream() {
  local name=$1
  local subjects=$2
  local max_msgs=$3
  output=$(nats stream add "$name" \
    --subjects "$subjects" \
    --max-msgs "$max_msgs" \
    --storage file \
    --discard old \
    --replicas 1 \
    -s "$NATS_URL" \
    -u "$NATS_USER" \
    -p "$NATS_PASS" \
    -n 2>&1)
  exit_code=$?
  
  if [ $exit_code -eq 0 ]; then
    echo "✓ Stream $name created"
  else
    # Check if stream already exists (exit code 1 with specific message)
    if echo "$output" | grep -q "stream name already in use"; then
      echo "ℹ  Stream $name already exists"
    else
      echo "WARNING: Stream $name creation returned exit code $exit_code"
      echo "  Output: $output"
    fi
  fi
}

echo "Creating JetStream streams..."
create_stream OVERLAY_UPDATES "overlay.>" 1000
create_stream UI_CONTROLS "ui.>" 500
create_stream MIDI_STATE "midi.>" 200
create_stream DATA_API "data.>" 500

echo "JetStream bootstrap complete"
# Verify streams were created (non-fatal if this fails)
nats stream list -s "$NATS_URL" -u "$NATS_USER" -p "$NATS_PASS" || echo "WARNING: Could not list streams"
