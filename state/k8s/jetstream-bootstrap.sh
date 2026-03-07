#!/bin/bash
set -e

NATS_URL="${NATS_URL:-nats://localhost:4222}"
MAX_RETRIES=30
RETRY_DELAY=2

echo "Waiting for NATS server to be ready..."
for attempt in $(seq 1 $MAX_RETRIES); do
  if nats server info -s "$NATS_URL" &>/dev/null; then
    echo "NATS server is ready"
    break
  fi
  if [ $attempt -eq $MAX_RETRIES ]; then
    echo "Failed to connect to NATS after $MAX_RETRIES attempts"
    exit 1
  fi
  echo "Attempt $attempt/$MAX_RETRIES: Waiting for NATS..."
  sleep $RETRY_DELAY
done

echo "Creating JetStream streams..."

# OVERLAY_UPDATES: State changes from overlay grid
# - Captures overlay.* publications
# - Retention: Keep last 1000 messages (grid state is important)
# - Storage: File-based for durability
nats stream add OVERLAY_UPDATES \
  --subjects "overlay.>" \
  --max-msgs 1000 \
  --storage file \
  --discard old \
  -s "$NATS_URL" \
  --replicas 1 \
  -n || echo "Stream OVERLAY_UPDATES already exists"

# UI_CONTROLS: UI layer publications (effects, settings, etc.)
# - Captures ui.* publications
# - Retention: Keep last 500 messages
# - Storage: File-based
nats stream add UI_CONTROLS \
  --subjects "ui.>" \
  --max-msgs 500 \
  --storage file \
  --discard old \
  -s "$NATS_URL" \
  --replicas 1 \
  -n || echo "Stream UI_CONTROLS already exists"

# MIDI_STATE: MIDI synth state and program changes
# - Captures midi.* publications
# - Retention: Keep last 200 messages (program state)
# - Storage: File-based
nats stream add MIDI_STATE \
  --subjects "midi.>" \
  --max-msgs 200 \
  --storage file \
  --discard old \
  -s "$NATS_URL" \
  --replicas 1 \
  -n || echo "Stream MIDI_STATE already exists"

# DATA_API: Data layer query results and cache updates
# - Captures data.* publications
# - Retention: Keep last 500 messages
# - Storage: File-based
nats stream add DATA_API \
  --subjects "data.>" \
  --max-msgs 500 \
  --storage file \
  --discard old \
  -s "$NATS_URL" \
  --replicas 1 \
  -n || echo "Stream DATA_API already exists"

echo "JetStream streams initialized successfully"
nats stream list -s "$NATS_URL"
