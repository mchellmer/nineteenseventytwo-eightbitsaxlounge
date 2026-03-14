// Popup image cycle for the panel-popup area.
//
// Default cycle: '8bsl' → 'engine' (label) → current engine value (e.g. 'room', 'lofi'), 7 s each.
// overlay.popup events interrupt for one 7-second slot then the cycle resumes.
// overlay.help triggers a sequential pass through HELP_IMAGES then resumes the default cycle.
// Images are loaded from /images/popups/<name>.png.

const POPUP_INTERVAL_MS = 7000;

// Help screens in display order; extend when new help images are added.
const HELP_IMAGES = ['help', 'help2', 'help3'];

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
  const items = _engineValue
    ? ['8bsl', 'engine', _engineValue]
    : ['8bsl', 'engine'];
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

// Cycle through all HELP_IMAGES in order (POPUP_INTERVAL_MS each) then resume the default cycle.
function triggerHelpCycle() {
  if (_popupTimer) clearTimeout(_popupTimer);
  let idx = 0;
  function nextHelp() {
    if (idx < HELP_IMAGES.length) {
      showPopup(HELP_IMAGES[idx++]);
      _popupTimer = setTimeout(nextHelp, POPUP_INTERVAL_MS);
    } else {
      _popupCycleIdx = 0;
      _cycle();
    }
  }
  nextHelp();
}

// Start (or restart) the default cycle.
function startPopupCycle() {
  if (_popupTimer) clearTimeout(_popupTimer);
  _popupCycleIdx = 0;
  _cycle();
}

if (typeof module !== 'undefined') module.exports = { showPopup, triggerPopup, startPopupCycle, setEngineValue, triggerHelpCycle };
