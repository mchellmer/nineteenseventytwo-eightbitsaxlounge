// Panel text and font-size helpers.
// Panel ids match TwitchConsole.json: engine, player, delay, time, dial1, dial2.

function adjustPanels() {
  document.querySelectorAll('.panel-text').forEach(el => {
    const h = el.getBoundingClientRect().height;
    let scale = 0.7;

    // Engine panel: step down scale for longer names to prevent overflow
    if (el.id === 'panel-engine') {
      const len = (el.textContent || '').length;
      if (len >= 8)      scale = 0.4;
      else if (len == 7) scale = 0.5;
      else if (len == 6) scale = 0.6;
    }

    el.style.fontSize = (h * scale) + 'px';
  });
}

function setPanelText(id, msg) {
  const el = document.getElementById('panel-' + id);
  if (!el) return;
  el.textContent = (msg && msg.data && msg.data.value != null)
    ? msg.data.value
    : (typeof msg === 'string' ? msg : JSON.stringify(msg.data || msg));
  adjustPanels();
}

if (typeof module !== 'undefined') module.exports = { adjustPanels, setPanelText };
