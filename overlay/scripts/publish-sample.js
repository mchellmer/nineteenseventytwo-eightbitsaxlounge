#!/usr/bin/env node
// Publish a sample ui.overlay.engine message to NATS (used by `make smoke`)
const { connect, StringCodec } = require('nats');

const NATS_URL = process.env.NATS_URL || 'nats://127.0.0.1:4222';
const SUBJECT = process.env.SUBJECT || 'ui.overlay.engine';
const PAYLOAD = process.env.PAYLOAD || JSON.stringify({ value: 'lofi', requester: 'smoke-test' });

async function main() {
  const nc = await connect({ servers: NATS_URL });
  const sc = StringCodec();
  nc.publish(SUBJECT, sc.encode(PAYLOAD));
  await nc.flush();
  console.log(`published -> ${SUBJECT} ${PAYLOAD} @ ${NATS_URL}`);
  await nc.close();
}

main().catch(err => {
  console.error(err);
  process.exit(1);
});
