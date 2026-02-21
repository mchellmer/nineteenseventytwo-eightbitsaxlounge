// Listens for socket.io events and updates DOM elements accordingly.
// * Events named `overlay.<field>` map to helper functions that set
//   text or image content based on the payload's `value` (if present).

const socket = io();

function setText(id, msg){
  const elt = document.getElementById(id);
  if(!elt) return;
  const v = (msg && msg.data && msg.data.value) ? msg.data.value : (typeof msg === 'string' ? msg : JSON.stringify(msg.data || msg));
  elt.textContent = v;
}

function getFolderForId(id){
  const map = { engine:'engine', time:'values', delay:'values', control1:'values', control2:'values' };
  return map[id] || id;
}

function setImage(id, msg){
  const img = document.getElementById(id+'-img');
  if(!img) return;
  const value = (msg && msg.data && msg.data.value) ? msg.data.value : (typeof msg === 'string' ? msg : JSON.stringify(msg.data || msg));
  const safe = encodeURIComponent(String(value));
  const folder = getFolderForId(id);
  img.src = `/images/${folder}/${safe}.svg`;
  img.onerror = () => { img.onerror=null; img.src='/images/error.svg'; };
}

// event handlers
socket.on('overlay.engine', msg=>setImage('engine',msg));
socket.on('overlay.time', msg=>setImage('time',msg));
socket.on('overlay.delay', msg=>setImage('delay',msg));
socket.on('overlay.control1', msg=>setImage('control1',msg));
socket.on('overlay.control2', msg=>setImage('control2',msg));
socket.on('overlay.player', msg=>setText('player',msg));
// generic logger
socket.onAny((evt,msg)=>console.debug('socket',evt,msg));
