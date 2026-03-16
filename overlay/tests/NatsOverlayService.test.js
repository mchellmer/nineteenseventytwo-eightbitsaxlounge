/**
 * Unit tests for src/services/NatsOverlayService.js
 *
 * The nats module is mocked so no real NATS server is required.
 */

jest.mock('nats');

const nats = require('nats');
const NatsOverlayService = require('../src/services/NatsOverlayService');

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

/**
 * Build a mock NATS subscription that yields the supplied messages then hangs.
 * @param {object[]} messages
 */
function mockSub(messages) {
  return {
    [Symbol.asyncIterator]() {
      let i = 0;
      return {
        async next() {
          if (i < messages.length) return { value: messages[i++], done: false };
          // Hang indefinitely to simulate an open subscription (never completes).
          return new Promise(() => {});
        },
      };
    },
  };
}

/** Build a raw NATS message object from a subject + JS payload. */
function makeMsg(subject, payload) {
  return { subject, data: Buffer.from(JSON.stringify(payload)) };
}

/** Build a mock io and nc pair that you can inspect afterwards. */
function makeIo() {
  return { on: jest.fn(), emit: jest.fn() };
}

/** Minimal NATS connection mock that replays messages on subscribe. */
function makeNc(messages) {
  return { subscribe: jest.fn().mockReturnValue(mockSub(messages)) };
}

// ---------------------------------------------------------------------------
// Set up StringCodec mock before each test
// ---------------------------------------------------------------------------

beforeEach(() => {
  jest.clearAllMocks();
  nats.StringCodec.mockReturnValue({
    decode: (data) => Buffer.from(data).toString(),
  });
});

// ---------------------------------------------------------------------------
// Tests
// ---------------------------------------------------------------------------

describe('NatsOverlayService.start()', () => {
  test('emits overlay.engine with the parsed payload', async () => {
    const nc = makeNc([makeMsg('overlay.engine', { value: 'room' })]);
    nats.connect.mockResolvedValue(nc);

    const io = makeIo();
    const svc = new NatsOverlayService({ logger: { info: jest.fn(), error: jest.fn() } });
    await svc.start(io);
    await new Promise(r => setTimeout(r, 50));

    expect(io.emit).toHaveBeenCalledWith('overlay.engine', expect.objectContaining({
      subject: 'overlay.engine',
      data: { value: 'room' },
    }));
  });

  test('emits overlay.time with a numeric value', async () => {
    const nc = makeNc([makeMsg('overlay.time', { value: 3 })]);
    nats.connect.mockResolvedValue(nc);

    const io = makeIo();
    const svc = new NatsOverlayService({ logger: { info: jest.fn(), error: jest.fn() } });
    await svc.start(io);
    await new Promise(r => setTimeout(r, 50));

    expect(io.emit).toHaveBeenCalledWith('overlay.time', expect.objectContaining({
      data: { value: 3 },
    }));
  });

  const allTails = ['engine', 'time', 'delay', 'dial1', 'dial2', 'player', 'help'];
  test.each(allTails)('correctly extracts tail for overlay.%s', async (tail) => {
    const nc = makeNc([makeMsg(`overlay.${tail}`, { value: 'x' })]);
    nats.connect.mockResolvedValue(nc);

    const io = makeIo();
    const svc = new NatsOverlayService({ logger: { info: jest.fn(), error: jest.fn() } });
    await svc.start(io);
    await new Promise(r => setTimeout(r, 50));

    expect(io.emit).toHaveBeenCalledWith(`overlay.${tail}`, expect.anything());
  });

  test('caches the last value per tail in lastState', async () => {
    const nc = makeNc([makeMsg('overlay.time', { value: 42 })]);
    nats.connect.mockResolvedValue(nc);

    const io = makeIo();
    const svc = new NatsOverlayService({ logger: { info: jest.fn(), error: jest.fn() } });
    await svc.start(io);
    await new Promise(r => setTimeout(r, 50));

    expect(svc.lastState['time']).toMatchObject({
      subject: 'overlay.time',
      data: { value: 42 },
    });
  });

  test('replays all cached state to a newly connected socket', async () => {
    const nc = makeNc([]);
    nats.connect.mockResolvedValue(nc);

    const io = makeIo();
    const svc = new NatsOverlayService({ logger: { info: jest.fn(), error: jest.fn() } });
    // Pre-seed state as if we had already received some messages
    svc.lastState['engine'] = { subject: 'overlay.engine', data: { value: 'lofi' } };
    svc.lastState['time']   = { subject: 'overlay.time',   data: { value: 7 } };
    await svc.start(io);

    // Simulate a new socket connection
    const [[, connectCb]] = io.on.mock.calls;
    const socket = { emit: jest.fn(), on: jest.fn() };
    connectCb(socket);

    expect(socket.emit).toHaveBeenCalledWith('overlay.engine', {
      subject: 'overlay.engine', data: { value: 'lofi' },
    });
    expect(socket.emit).toHaveBeenCalledWith('overlay.time', {
      subject: 'overlay.time', data: { value: 7 },
    });
  });

  test('overwrites lastState when a second message arrives for the same tail', async () => {
    const nc = makeNc([
      makeMsg('overlay.delay', { value: 1 }),
      makeMsg('overlay.delay', { value: 9 }),
    ]);
    nats.connect.mockResolvedValue(nc);

    const io = makeIo();
    const svc = new NatsOverlayService({ logger: { info: jest.fn(), error: jest.fn() } });
    await svc.start(io);
    await new Promise(r => setTimeout(r, 50));

    expect(svc.lastState['delay'].data).toEqual({ value: 9 });
  });

  test('does not throw when the NATS payload is not valid JSON', async () => {
    const nc = makeNc([{ subject: 'overlay.engine', data: Buffer.from('not-json') }]);
    nats.connect.mockResolvedValue(nc);

    const io = makeIo();
    const logger = { info: jest.fn(), error: jest.fn() };
    const svc = new NatsOverlayService({ logger });
    await svc.start(io);
    await new Promise(r => setTimeout(r, 50));

    // payload is stored as a raw string fallback — should not throw
    expect(io.emit).toHaveBeenCalledWith('overlay.engine', expect.objectContaining({
      data: 'not-json',
    }));
  });
});
