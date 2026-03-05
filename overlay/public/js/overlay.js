// Listens for socket.io events and updates DOM elements accordingly.
// * Events named `overlay.<field>` set the textContent of panel-text divs.
// * Panel ids match TwitchConsole.json: delay, engine, player, time, dial1, dial2.
//
// Pure functions (adjustPanels, setPanelText) are exported for unit testing.
// The browser auto-calls init(); Node/Jest imports the module without running it.

function adjustPanels() {
  document.querySelectorAll('.panel-text').forEach(el => {
    const h = el.getBoundingClientRect().height;
    // choose a scale factor that makes the digits occupy most of the box
    el.style.fontSize = (h * 0.7) + 'px';
  });
}

function setPanelText(id, msg){
  const elt = document.getElementById('panel-' + id);
  if(!elt) return;
  elt.textContent = (msg && msg.data && msg.data.value != null)
    ? msg.data.value
    : (typeof msg === 'string' ? msg : JSON.stringify(msg.data || msg));
  adjustPanels();
}

function init() {
  const socket = io(); // io is a browser global from the socket.io CDN script
  socket.on('overlay.engine', msg => setPanelText('engine', msg));
  socket.on('overlay.time',   msg => setPanelText('time',   msg));
  socket.on('overlay.delay',  msg => setPanelText('delay',  msg));
  socket.on('overlay.dial1',  msg => setPanelText('dial1',  msg));
  socket.on('overlay.dial2',  msg => setPanelText('dial2',  msg));
  socket.on('overlay.player', msg => setPanelText('player', msg));
  socket.onAny((evt, msg) => console.debug('socket', evt, msg));

  window.addEventListener('resize', adjustPanels);
  window.addEventListener('DOMContentLoaded', adjustPanels);
}

// In the browser there is no `module` (no bundler); auto-start.
// In Node / Jest `module` is defined; export for testing instead.
if (typeof module !== 'undefined') {
  module.exports = { setPanelText, adjustPanels, init };
} else {
  init();
}
