/**
 * @jest-environment jsdom
 */

const { showPopup, triggerPopup, startPopupCycle, setEngineValue } = require('../public/js/popup.js');

const POPUP_HTML = '<img id="panel-popup" src="" />';

beforeEach(() => { document.body.innerHTML = POPUP_HTML; });

describe('showPopup', () => {
  test('sets img src and makes element visible', () => {
    showPopup('room');
    const el = document.getElementById('panel-popup');
    expect(el.src).toContain('/images/popups/room.png');
    expect(el.style.display).toBe('block');
  });

  test('does not throw when panel-popup is absent', () => {
    document.body.innerHTML = '';
    expect(() => showPopup('room')).not.toThrow();
  });
});

describe('triggerPopup', () => {
  beforeEach(() => jest.useFakeTimers());
  afterEach(()  => jest.useRealTimers());

  test('shows the named image immediately', () => {
    triggerPopup('hall');
    expect(document.getElementById('panel-popup').src).toContain('/images/popups/hall.png');
  });

  test('resumes the cycle after one interval', () => {
    triggerPopup('hall');
    jest.advanceTimersByTime(5000);
    expect(document.getElementById('panel-popup').src).toContain('/images/popups/engine.png');
  });
});

describe('startPopupCycle', () => {
  beforeEach(() => jest.useFakeTimers());
  afterEach(()  => jest.useRealTimers());

  test('shows engine on the first tick', () => {
    startPopupCycle();
    expect(document.getElementById('panel-popup').src).toContain('/images/popups/engine.png');
  });

  test('stays on engine when no engine value is known', () => {
    setEngineValue(null);
    startPopupCycle();
    jest.advanceTimersByTime(5000);
    expect(document.getElementById('panel-popup').src).toContain('/images/popups/engine.png');
  });

  test('alternates to engine value once one is set', () => {
    setEngineValue('room');
    startPopupCycle();
    // first tick: engine, second tick: room
    jest.advanceTimersByTime(5000);
    expect(document.getElementById('panel-popup').src).toContain('/images/popups/room.png');
  });
});
