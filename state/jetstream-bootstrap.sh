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

nats_cmd() {
  nats --server "$NATS_URL" --user "$NATS_USER" --password "$NATS_PASS" "$@"
}

# Wait for NATS to be ready using wget against monitoring endpoint
log "Waiting for NATS server..."
attempt=1
while [ $attempt -le $MAX_RETRIES ]; do
  if wget -q -O /dev/null http://127.0.0.1:8222/varz 2>/dev/null; then
    log "NATS monitoring endpoint ready"
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

# Verify nats CLI can authenticate
log "Testing nats CLI authentication..."
if ! nats_cmd stream ls >/dev/null 2>&1; then
  log "WARNING: nats CLI stream ls failed, retrying once..."
  sleep 2
  if ! nats_cmd stream ls >/dev/null 2>&1; then
    log "ERROR: nats CLI cannot connect/authenticate"
    nats_cmd stream ls 2>&1 | while IFS= read -r line; do log "  $line"; done
    exit 1
  fi
fi
log "Authentication OK"

log "Creating JetStream streams..."

# Create OVERLAY_UPDATES stream
output=$(nats_cmd stream add OVERLAY_UPDATES --subjects "overlay.>" --max-msgs 1000 --storage file --discard old --replicas 1 --defaults 2>&1)
if [ $? -eq 0 ]; then
  log "✓ OVERLAY_UPDATES created"
elif nats_cmd stream info OVERLAY_UPDATES >/dev/null 2>&1; then
  log "ℹ OVERLAY_UPDATES exists"
else
  log "WARNING: OVERLAY_UPDATES failed: $output"
fi

# Create UI_CONTROLS stream
output=$(nats_cmd stream add UI_CONTROLS --subjects "ui.>" --max-msgs 500 --storage file --discard old --replicas 1 --defaults 2>&1)
if [ $? -eq 0 ]; then
  log "✓ UI_CONTROLS created"
elif nats_cmd stream info UI_CONTROLS >/dev/null 2>&1; then
  log "ℹ UI_CONTROLS exists"
else
  log "WARNING: UI_CONTROLS failed: $output"
fi

# Create MIDI_STATE stream
output=$(nats_cmd stream add MIDI_STATE --subjects "midi.>" --max-msgs 200 --storage file --discard old --replicas 1 --defaults 2>&1)
if [ $? -eq 0 ]; then
  log "✓ MIDI_STATE created"
elif nats_cmd stream info MIDI_STATE >/dev/null 2>&1; then
  log "ℹ MIDI_STATE exists"
else
  log "WARNING: MIDI_STATE failed: $output"
fi

# Create DATA_API stream
output=$(nats_cmd stream add DATA_API --subjects "data.>" --max-msgs 500 --storage file --discard old --replicas 1 --defaults 2>&1)
if [ $? -eq 0 ]; then
  log "✓ DATA_API created"
elif nats_cmd stream info DATA_API >/dev/null 2>&1; then
  log "ℹ DATA_API exists"
else
  log "WARNING: DATA_API failed: $output"
fi

log "JetStream bootstrap complete"
exit 0

