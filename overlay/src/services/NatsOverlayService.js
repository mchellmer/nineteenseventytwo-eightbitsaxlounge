/**
 * NatsOverlayService.js
 * ---------------------
 * OverlayService implementationthat uses a NATS server as the event source.  It
 * listens on all `ui.overlay.*` subjects, decodes the payload, caches the most
 * recent value per-tail, and forwards updates to connected browser clients via
 * Socket.IO.
 *
 * Configuration options:
 *  - natsUrl: URL of the NATS server (default: process.env.NATS_URL)
 *  - logger: object with .info/.error methods for logging
 */

const { connect, StringCodec } = require('nats');
const OverlayService = require('./OverlayService');

class NatsOverlayService extends OverlayService {
  /**
   * @param {object} [options]
   * @param {string} [options.natsUrl] - NATS server address
   * @param {{info:Function,error:Function}} [options.logger] - logging API
   */
  constructor(options = {}) {
    super();
    this.natsUrl = options.natsUrl || process.env.NATS_URL || 'nats://nats:4222';
    this.logger = options.logger || console;
    // cache map: tail -> { subject, data }
    this.lastState = Object.create(null);
  }

  /**
   * @param {import('socket.io').Server} io
   */
  async start(io) {
    const logger = this.logger;
    const lastState = this.lastState;

    // Attach listener for new Socket.IO connections and emit the most recent values to retain state.
    io.on('connection', (socket) => {
      logger.info('socket connected', socket.id);
      Object.keys(lastState).forEach((tail) => {
        const entry = lastState[tail];
        socket.emit(`overlay.${tail}`, { subject: entry.subject, data: entry.data });
      });
      socket.on('disconnect', () => logger.info('socket disconnected', socket.id));
    });

    // Connect to NATS and subscribe to overlay events.
    const nc = await connect({ servers: this.natsUrl });
    logger.info('connected to NATS', this.natsUrl);
    const sc = StringCodec();

    // jetstream / wildcard subscribe
    const sub = nc.subscribe('ui.overlay.*');

    // Process incoming messages and forward to Socket.IO clients
    (async () => {
      for await (const m of sub) {
        try {
          const subj = m.subject;
          const payload = sc.decode(m.data);
          let data = null;
          try { data = JSON.parse(payload); } catch { data = payload; }
          logger.info('nats msg', subj, data);

          // compute the tail portion of the subject and propagate
          const tail = subj.split('.').slice(2).join('.');
          const event = `overlay.${tail}`;

          // cache & forward
          lastState[tail] = { subject: subj, data };
          logger.info('emit event', event, data);
          io.emit(event, { subject: subj, data });
        } catch (err) {
          logger.error('failed processing nats message', err);
        }
      }
    })().catch(err => logger.error('subscription error', err));
  }
}

module.exports = NatsOverlayService;
