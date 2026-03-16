/**
 * @jest-environment jsdom
 */

const { showPopup, triggerPopup, startPopupCycle, setEngineValue, triggerHelpCycle } = require('../public/js/popup.js');

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
    jest.advanceTimersByTime(7000);
    expect(document.getElementById('panel-popup').src).toContain('/images/popups/8bsl.png');
  });
});

describe('startPopupCycle', () => {
  beforeEach(() => jest.useFakeTimers());
  afterEach(()  => jest.useRealTimers());

  test('shows 8bsl on the first tick', () => {
    startPopupCycle();
    expect(document.getElementById('panel-popup').src).toContain('/images/popups/8bsl.png');
  });

  test('advances to engine after one interval when no engine value is known', () => {
    setEngineValue(null);
    startPopupCycle();
    jest.advanceTimersByTime(7000);
    expect(document.getElementById('panel-popup').src).toContain('/images/popups/engine.png');
  });

  test('advances through 8bsl then engine then engine value', () => {
    setEngineValue('room');
    startPopupCycle();
    // first tick: 8bsl, second tick: engine, third tick: room
    jest.advanceTimersByTime(7000);
    expect(document.getElementById('panel-popup').src).toContain('/images/popups/engine.png');
    jest.advanceTimersByTime(7000);
    expect(document.getElementById('panel-popup').src).toContain('/images/popups/room.png');
  });
});

describe('triggerHelpCycle', () => {
  beforeEach(() => jest.useFakeTimers());
  afterEach(()  => jest.useRealTimers());

  test('shows help on the first tick', () => {
    triggerHelpCycle();
    expect(document.getElementById('panel-popup').src).toContain('/images/popups/help.png');
  });

  test('advances through help2 and help3 in order', () => {
    triggerHelpCycle();
    jest.advanceTimersByTime(7000);
    expect(document.getElementById('panel-popup').src).toContain('/images/popups/help2.png');
    jest.advanceTimersByTime(7000);
    expect(document.getElementById('panel-popup').src).toContain('/images/popups/help3.png');
  });

  test('resumes the default cycle after all help images', () => {
    triggerHelpCycle();
    jest.advanceTimersByTime(7000 * 3);
    expect(document.getElementById('panel-popup').src).toContain('/images/popups/8bsl.png');
  });
});
