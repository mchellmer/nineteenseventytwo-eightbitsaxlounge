/**
 * Entry point for the overlay service.
 *
 * HTTP Framework: Express, creates a server and serves static files from `public/` etc.
 * HTTP Server: http, used to create a server instance to accept incoming TCP connections.
 * Websocket Framework: Socket.IO, used to manage websocket connections and broadcast messages to clients.
 * 
 * This module configures an Express server, attaches a Socket.IO instance,
 * and then delegates runtime behaviour to a OverlayService implementation
 * (`NatsOverlayService`).  The only responsibilities here are:
*/

const express = require('express');
const http = require('http');
const { Server } = require('socket.io');
const NatsOverlayService = require('./services/NatsOverlayService');

const PORT = process.env.PORT || 3000;
const logger = console;

async function start() {
  /**
   * Set up the Express server and Socket.IO instance.  The server serves static
   * files from the `public` directory (the overlay frontend) and listens for
   * Socket.IO connections to broadcast overlay updates to.
   */
  // Configure server
  const app = express();
  const server = http.createServer(app);
  const io = new Server(server, { cors: { origin: '*' } });

  app.use(express.static('public'));

  server.listen(PORT, () => logger.info(`overlay: listening on ${PORT}`));

  // Configure 8bitsaxlounge logic to update the broadcast view
  const overlay = new NatsOverlayService({ logger });
  await overlay.start(io);
}

start().catch(err => {
  logger.error(err);
  process.exit(1);
});
