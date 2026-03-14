// Popup image cycle for the panel-popup area.
//
// Default cycle: 'engine' (label) → current engine value (e.g. 'room', 'lofi'), 5 s each.
// overlay.popup events interrupt for one 5-second slot then the cycle resumes.
// Images are loaded from /images/popups/<name>.png.

const POPUP_INTERVAL_MS = 5000;

let _engineValue   = null;
let _popupTimer    = null;
let _popupCycleIdx = 0;

function showPopup(name) {
  const el = document.getElementById('panel-popup');
  if (!el) return;
  el.src = '/images/popups/' + name + '.png';
  el.style.display = 'block';
}

function setEngineValue(val) {
  _engineValue = val;
}

function _cycle() {
  const items = (_engineValue && _engineValue !== 'engine')
    ? ['engine', _engineValue]
    : ['engine'];
  showPopup(items[_popupCycleIdx % items.length]);
  _popupCycleIdx = (_popupCycleIdx + 1) % items.length;
  _popupTimer = setTimeout(_cycle, POPUP_INTERVAL_MS);
}

// Show a named popup for one interval, then resume the default cycle.
function triggerPopup(name) {
  if (_popupTimer) clearTimeout(_popupTimer);
  showPopup(name);
  _popupCycleIdx = 0;
  _popupTimer = setTimeout(_cycle, POPUP_INTERVAL_MS);
}

// Start (or restart) the default cycle.
function startPopupCycle() {
  if (_popupTimer) clearTimeout(_popupTimer);
  _popupCycleIdx = 0;
  _cycle();
}

if (typeof module !== 'undefined') module.exports = { showPopup, triggerPopup, startPopupCycle, setEngineValue };
