# Stage 1: grab nats-cli from nats-box (multi-arch)
FROM natsio/nats-box:latest AS natscli

# Stage 2: build custom NATS image with config and bootstrap script baked in
FROM nats:2-alpine

# Add the nats CLI so the bootstrap script can create JetStream streams
COPY --from=natscli /usr/local/bin/nats /usr/local/bin/nats

# Config
COPY nats.conf /etc/nats/nats.conf
COPY jetstream-bootstrap.sh /usr/local/bin/jetstream-bootstrap.sh
RUN chmod +x /usr/local/bin/jetstream-bootstrap.sh

# Passwords are injected at runtime via environment variables:
#   OVERLAY_PASS, UI_PASS, MIDI_PASS, DATA_PASS
# These are referenced in nats.conf using NATS native $VAR substitution.

EXPOSE 4222 8222 6222
