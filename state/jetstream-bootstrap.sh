#!/bin/sh
# JetStream stream bootstrap
# Runs as a postStart lifecycle hook to create domain streams after NATS starts

NATS_URL="${NATS_URL:-nats://127.0.0.1:4222}"
NATS_USER="${NATS_USER:-system}"
NATS_PASS="${NATS_PASS:-$SYSTEM_PASS}"
MAX_RETRIES=30
RETRY_DELAY=1

log() {
  echo "[BOOTSTRAP] $*" >&2
}

log "Starting JetStream bootstrap"
log "NATS_URL: $NATS_URL"

# Validate required credentials
if [ -z "$NATS_PASS" ]; then
  log "ERROR: NATS_PASS or SYSTEM_PASS not set"
  exit 1
fi

# Wait for NATS to be ready by checking the monitoring endpoint
log "Waiting for NATS server..."
attempt=1
while [ $attempt -le $MAX_RETRIES ]; do
  if curl -s http://127.0.0.1:8222/varz > /dev/null 2>&1; then
    log "NATS server ready"
    break
  fi
  if [ $attempt -eq $MAX_RETRIES ]; then
    log "ERROR: Max retries reached, NATS not responding"
    exit 1
  fi
  log "Attempt $attempt/$MAX_RETRIES"
  sleep $RETRY_DELAY
  attempt=$((attempt + 1))
done

log "Creating JetStream streams..."

# Create OVERLAY_UPDATES stream
if nats --creds=/dev/null -s "$NATS_URL" -u "$NATS_USER" -p "$NATS_PASS" stream add OVERLAY_UPDATES --subjects "overlay.>" --max-msgs 1000 --storage file --discard old --replicas 1 -n >/dev/null 2>&1; then
  log "✓ OVERLAY_UPDATES created"
elif nats --creds=/dev/null -s "$NATS_URL" -u "$NATS_USER" -p "$NATS_PASS" stream info OVERLAY_UPDATES >/dev/null 2>&1; then
  log "ℹ OVERLAY_UPDATES exists"
else
  log "WARNING: OVERLAY_UPDATES creation/check failed"
fi

# Create UI_CONTROLS stream
if nats --creds=/dev/null -s "$NATS_URL" -u "$NATS_USER" -p "$NATS_PASS" stream add UI_CONTROLS --subjects "ui.>" --max-msgs 500 --storage file --discard old --replicas 1 -n >/dev/null 2>&1; then
  log "✓ UI_CONTROLS created"
elif nats --creds=/dev/null -s "$NATS_URL" -u "$NATS_USER" -p "$NATS_PASS" stream info UI_CONTROLS >/dev/null 2>&1; then
  log "ℹ UI_CONTROLS exists"
else
  log "WARNING: UI_CONTROLS creation/check failed"
fi

# Create MIDI_STATE stream
if nats --creds=/dev/null -s "$NATS_URL" -u "$NATS_USER" -p "$NATS_PASS" stream add MIDI_STATE --subjects "midi.>" --max-msgs 200 --storage file --discard old --replicas 1 -n >/dev/null 2>&1; then
  log "✓ MIDI_STATE created"
elif nats --creds=/dev/null -s "$NATS_URL" -u "$NATS_USER" -p "$NATS_PASS" stream info MIDI_STATE >/dev/null 2>&1; then
  log "ℹ MIDI_STATE exists"
else
  log "WARNING: MIDI_STATE creation/check failed"
fi

# Create DATA_API stream
if nats --creds=/dev/null -s "$NATS_URL" -u "$NATS_USER" -p "$NATS_PASS" stream add DATA_API --subjects "data.>" --max-msgs 500 --storage file --discard old --replicas 1 -n >/dev/null 2>&1; then
  log "✓ DATA_API created"
elif nats --creds=/dev/null -s "$NATS_URL" -u "$NATS_USER" -p "$NATS_PASS" stream info DATA_API >/dev/null 2>&1; then
  log "ℹ DATA_API exists"
else
  log "WARNING: DATA_API creation/check failed"
fi

log "JetStream bootstrap complete"
exit 0

