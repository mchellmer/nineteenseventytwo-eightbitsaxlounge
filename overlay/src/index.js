const express = require('express');
const http = require('http');
const { connect, StringCodec } = require('nats');
const { Server } = require('socket.io');

const PORT = process.env.PORT || 3000;
const NATS_URL = process.env.NATS_URL || 'nats://nats:4222';

async function start() {
  const app = express();
  const server = http.createServer(app);
  const io = new Server(server, { cors: { origin: '*' } });

  app.use(express.static('public'));

  server.listen(PORT, () => console.log(`overlay: listening on ${PORT}`));

  // Socket.IO connection handling
  io.on('connection', (socket) => {
    console.log('socket connected', socket.id);
    socket.on('disconnect', () => console.log('socket disconnected', socket.id));
  });

  // Connect to NATS
  const nc = await connect({ servers: NATS_URL });
  console.log('connected to NATS', NATS_URL);

  const sc = StringCodec();

  // Subscribe to overlay topics
  const sub = nc.subscribe('ui.overlay.*');
  (async () => {
    for await (const m of sub) {
      try {
        const subj = m.subject; // e.g. ui.overlay.engine
        const payload = sc.decode(m.data);
        let data = null;
        try { data = JSON.parse(payload); } catch { data = payload; }
        console.log('nats msg', subj, data);

        // emit to connected browsers using subject tail as event name
        const event = subj.split('.').slice(2).join('.'); // overlay.engine
        io.emit(event, { subject: subj, data });
      } catch (err) {
        console.error('failed processing nats message', err);
      }
    }
  })().catch(err => console.error('subscription error', err));
}

start().catch(err => {
  console.error(err);
  process.exit(1);
});
