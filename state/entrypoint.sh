#!/bin/sh
# Entrypoint for NATS container
# Substitutes environment variables into nats.conf and starts NATS server

set -e

# Verify required environment variables are set
if [ -z "$SYSTEM_PASS" ] || [ -z "$OVERLAY_PASS" ] || [ -z "$CHAT_PASS" ] || [ -z "$MIDI_PASS" ] || [ -z "$DATA_PASS" ]; then
  echo "ERROR: Missing required password environment variables"
  echo "Required: SYSTEM_PASS, OVERLAY_PASS, CHAT_PASS, MIDI_PASS, DATA_PASS"
  exit 1
fi

# Create config directory if needed
mkdir -p /etc/nats

# Substitute environment variables into the config template
# Using sed to replace $VAR_NAME with actual values
sed \
  -e "s|\$SYSTEM_PASS|$SYSTEM_PASS|g" \
  -e "s|\$OVERLAY_PASS|$OVERLAY_PASS|g" \
  -e "s|\$CHAT_PASS|$CHAT_PASS|g" \
  -e "s|\$MIDI_PASS|$MIDI_PASS|g" \
  -e "s|\$DATA_PASS|$DATA_PASS|g" \
  /etc/nats/nats.conf.template > /etc/nats/nats.conf

echo "✓ Configuration generated with substituted passwords"

# Execute NATS with the generated config
exec nats-server "$@"
