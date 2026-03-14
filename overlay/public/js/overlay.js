// Socket.IO event wiring — connects incoming overlay.* events to panel and popup updates.
// Depends on panels.js (setPanelText) and popup.js (setEngineValue, triggerPopup,
// startPopupCycle) being loaded first via <script> tags.

function init() {
  const socket = io();

  socket.on('overlay.engine', msg => {
    setPanelText('engine', msg);
    const val = (msg && msg.data && msg.data.value != null) ? String(msg.data.value) : null;
    if (val) setEngineValue(val);
  });

  socket.on('overlay.time',      msg => setPanelText('time',   msg));
  socket.on('overlay.delay',     msg => setPanelText('delay',  msg));
  socket.on('overlay.predelay',  msg => setPanelText('delay',  msg));
  socket.on('overlay.dial1',     msg => setPanelText('dial1',  msg));
  socket.on('overlay.control1',  msg => setPanelText('dial1',  msg));
  socket.on('overlay.dial2',     msg => setPanelText('dial2',  msg));
  socket.on('overlay.control2',  msg => setPanelText('dial2',  msg));
  socket.on('overlay.player',    msg => setPanelText('player', msg));

  socket.on('overlay.popup', msg => {
    const val = (msg && msg.data && msg.data.value != null) ? String(msg.data.value) : null;
    if (val) triggerPopup(val);
  });

  socket.onAny((evt, msg) => console.debug('socket', evt, msg));

  window.addEventListener('resize', adjustPanels);
  window.addEventListener('DOMContentLoaded', adjustPanels);

  startPopupCycle();
}

if (typeof module !== 'undefined') module.exports = { init };
else init();
