/**
 * @jest-environment jsdom
 */

const { adjustPanels, setPanelText } = require('../public/js/panels.js');

const PANEL_HTML = `
  <div id="panel-engine" class="panel-text"></div>
  <div id="panel-player" class="panel-text"></div>
  <div id="panel-time"   class="panel-text"></div>
  <div id="panel-delay"  class="panel-text"></div>
  <div id="panel-dial1"  class="panel-text"></div>
  <div id="panel-dial2"  class="panel-text"></div>
`;

beforeEach(() => { document.body.innerHTML = PANEL_HTML; });

describe('setPanelText', () => {
  test('sets text from msg.data.value', () => {
    setPanelText('engine', { data: { value: 'room' } });
    expect(document.getElementById('panel-engine').textContent).toBe('room');
  });

  test('sets text when msg.data.value is a number', () => {
    setPanelText('time', { data: { value: 3 } });
    expect(document.getElementById('panel-time').textContent).toBe('3');
  });

  test('treats value of 0 as valid (falsy but not null)', () => {
    setPanelText('dial1', { data: { value: 0 } });
    expect(document.getElementById('panel-dial1').textContent).toBe('0');
  });

  test('sets text when msg is a plain string', () => {
    setPanelText('player', 'MCH');
    expect(document.getElementById('panel-player').textContent).toBe('MCH');
  });

  test('falls back to JSON when msg.data has no value field', () => {
    setPanelText('engine', { data: { status: 'ok' } });
    expect(document.getElementById('panel-engine').textContent).toBe('{"status":"ok"}');
  });

  test('falls back to JSON when msg.data.value is null', () => {
    setPanelText('delay', { data: { value: null } });
    expect(document.getElementById('panel-delay').textContent).toBe('{"value":null}');
  });

  test('does not throw when panel element does not exist', () => {
    expect(() => setPanelText('nonexistent', { data: { value: 'x' } })).not.toThrow();
  });

  test('all panel ids resolve to an element', () => {
    ['engine', 'player', 'time', 'delay', 'dial1', 'dial2'].forEach(id => {
      setPanelText(id, { data: { value: id } });
      expect(document.getElementById('panel-' + id).textContent).toBe(id);
    });
  });
});

describe('adjustPanels', () => {
  test('sets font-size to 70% of the element height', () => {
    const el = document.getElementById('panel-time');
    jest.spyOn(el, 'getBoundingClientRect').mockReturnValue({ height: 100 });
    adjustPanels();
    expect(el.style.fontSize).toBe('70px');
  });

  test('leaves font-size at 0px when panel height is 0 (not yet laid out)', () => {
    adjustPanels();
    expect(document.getElementById('panel-engine').style.fontSize).toBe('0px');
  });

  test('applies the factor independently to each panel', () => {
    const heights = { engine: 80, time: 60, delay: 40 };
    ['engine', 'time', 'delay'].forEach(id => {
      jest.spyOn(document.getElementById('panel-' + id), 'getBoundingClientRect')
        .mockReturnValue({ height: heights[id] });
    });
    adjustPanels();
    expect(document.getElementById('panel-engine').style.fontSize).toBe('56px');
    expect(document.getElementById('panel-time').style.fontSize).toBe('42px');
    expect(document.getElementById('panel-delay').style.fontSize).toBe('28px');
  });
});
