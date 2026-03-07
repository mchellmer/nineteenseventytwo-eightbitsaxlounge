# nats-cli to be used for jetstream management in the entrypoint script
FROM natsio/nats-box:latest AS natscli

# state image
FROM nats:2-alpine

# Add the nats CLI so the bootstrap script can create JetStream streams
COPY --from=natscli /usr/local/bin/nats /usr/local/bin/nats

# Entrypoint script for credential management and config generation
COPY entrypoint.sh /usr/local/bin/entrypoint.sh
RUN chmod +x /usr/local/bin/entrypoint.sh

COPY jetstream-bootstrap.sh /usr/local/bin/jetstream-bootstrap.sh
RUN chmod +x /usr/local/bin/jetstream-bootstrap.sh

COPY nats.conf.template /etc/nats/nats.conf.template

# Env vars expected:
#   SYSTEM_PASS, OVERLAY_PASS, UI_PASS, MIDI_PASS, DATA_PASS

EXPOSE 4222 8222 6222
ENTRYPOINT ["/usr/local/bin/entrypoint.sh"]
