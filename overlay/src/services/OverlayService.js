/**
 * Abstract base class representing an overlay event service.  Concrete
 * implementations (NATS, MQTT, etc.) should subclass this and provide
 * their own `start(io)` method.  The purpose of the service is to receive
 * messages from some backend/pub‑sub and relay them to connected browsers
 * via a Socket.IO server.
 */
class OverlayService {
  /**
   * Begin servicing overlay clients.  Implementations should attach any
   * listeners to the supplied Socket.IO `io` instance and return a Promise
   * that resolves when startup is complete.
   *
   * @param {import('socket.io').Server} io - socket server used to emit events
   * @returns {Promise<void>}
   */
  async start(io) {
    throw new Error('start() not implemented');
  }
}

module.exports = OverlayService;
