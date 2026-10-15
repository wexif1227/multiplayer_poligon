'use strict';

const http = require('http');
const crypto = require('crypto');
const WebSocket = require('ws');

const PORT = Number(process.env.PORT || 3000);
const MAX_PLAYERS = 6;
const SPAWN_SHIELD_MS = 3000;
const STATE_INTERVAL_MS = 35;
const MAX_PAYLOAD = 16 * 1024;

const SPAWN_POINTS = [
  [-16, 1.6, -22],
  [16, 1.6, -22],
  [-18, 1.6, 20],
  [18, 1.6, 20],
  [0, 1.6, 24]
];

const CHARACTER_MODELS = new Set([
  'chicken', 'anime3d', 'anime1', 'anime2', 'anime3', 'annie', 'bikini', 'elf'
]);
const WEAPON_ACTIONS = new Set([
  'reload_start', 'reload_end', 'inspect_start', 'inspect_cancel', 'melee'
]);
const ALLOWED_MODES = new Set(['training', 'rampage', 'pigmode']);
const ALLOWED_MAPS = new Set(['dust', 'mirage', 'standoff']);

const rooms = new Map();
const clients = new Map();

const clean = (value, max = 18, fallback = 'Игрок') =>
  String(value || '').replace(/[<>\u0000-\u001f]/g, '').trim().slice(0, max) || fallback;

const cleanModel = (value, fallback = 'chicken') => {
  const id = String(value || '').trim().toLowerCase();
  return CHARACTER_MODELS.has(id) ? id : fallback;
};

const num = (value, fallback = 0) => {
  const parsed = Number(value);
  return Number.isFinite(parsed) ? parsed : fallback;
};

const randomSpawn = () => {
  const s = SPAWN_POINTS[Math.floor(Math.random() * SPAWN_POINTS.length)];
  return {
    x: s[0] + (Math.random() - 0.5) * 1.8,
    y: s[1],
    z: s[2] + (Math.random() - 0.5) * 1.8
  };
};

const publicPlayer = p => ({
  id: p.id,
  name: p.name,
  x: p.x,
  y: p.y,
  z: p.z,
  yaw: p.yaw,
  pitch: p.pitch,
  weapon: p.weapon,
  model: p.model || 'chicken',
  hp: p.hp,
  kills: p.kills,
  deaths: p.deaths,
  godMode: Boolean(p.godMode),
  crouched: Boolean(p.crouched),
  spawnShieldMs: Math.max(0, (p.spawnShieldUntil || 0) - Date.now())
});

const send = (ws, message) => {
  if (ws.readyState === WebSocket.OPEN) ws.send(JSON.stringify(message));
};

function roomPlayers(room) {
  return [...room.players.values()].map(publicPlayer);
}

function roomInfo(room) {
  return { mode: room.mode || 'training', map: room.map || 'dust' };
}

function broadcast(room, message, except = null) {
  for (const player of room.players.values()) {
    if (player.ws !== except) send(player.ws, message);
  }
}

function announceRoster(room) {
  broadcast(room, { type: 'players', players: roomPlayers(room) });
}

function leave(ws) {
  const p = clients.get(ws);
  if (!p) return;

  clients.delete(ws);
  const room = rooms.get(p.room);
  if (!room) return;

  room.players.delete(p.id);
  if (!room.players.size) rooms.delete(p.room);
  else announceRoster(room);
}

const server = http.createServer((req, res) => {
  if (req.url === '/' || req.url === '/health') {
    res.writeHead(200, {
      'content-type': 'text/plain; charset=utf-8',
      'cache-control': 'no-store'
    });
    res.end('Poligon friends server is running. WebSocket endpoint is the root URL.');
    return;
  }

  if (req.url === '/version') {
    res.writeHead(200, {
      'content-type': 'application/json; charset=utf-8',
      'cache-control': 'no-store'
    });
    res.end(JSON.stringify({
      name: 'poligon-friends-server',
      version: '1.1.0',
      features: ['character-model-sync', 'weapon-actions', 'crouch-sync', 'spawn-shield']
    }));
    return;
  }

  res.writeHead(404, { 'content-type': 'text/plain; charset=utf-8' });
  res.end('Not found');
});

const wss = new WebSocket.Server({ server, maxPayload: MAX_PAYLOAD });

wss.on('connection', ws => {
  ws.on('message', raw => {
    let m;
    try { m = JSON.parse(raw.toString()); } catch { return; }
    if (!m || typeof m.type !== 'string') return;

    let p = clients.get(ws);

    if (m.type === 'join') {
      if (p) return;

      const roomCode = clean(m.room, 12, 'KENTY')
        .toUpperCase().replace(/[^A-Z0-9_-]/g, '').slice(0, 12) || 'KENTY';

      let room = rooms.get(roomCode);
      if (!room) {
        room = { players: new Map(), mode: 'training', map: 'dust' };
        rooms.set(roomCode, room);
      }

      if (room.players.size >= MAX_PLAYERS) {
        send(ws, { type: 'error', message: 'Комната заполнена (максимум 6 игроков).' });
        ws.close(1008, 'room full');
        return;
      }

      const id = crypto.randomUUID();
      const spawn = randomSpawn();
      p = {
        id,
        ws,
        room: roomCode,
        name: clean(m.name),
        model: cleanModel(m.model),
        ...spawn,
        yaw: 0,
        pitch: 0,
        weapon: 'ak47',
        hp: 100,
        godMode: false,
        crouched: false,
        kills: 0,
        deaths: 0,
        lastState: 0,
        spawnShieldUntil: Date.now() + SPAWN_SHIELD_MS
      };

      room.players.set(id, p);
      clients.set(ws, p);
      send(ws, { type: 'welcome', id, room: roomCode, players: roomPlayers(room), ...roomInfo(room) });
      announceRoster(room);
      return;
    }

    if (!p) {
      send(ws, { type: 'error', message: 'Сначала войди в комнату.' });
      return;
    }

    const room = rooms.get(p.room);
    if (!room) return;

    if (m.type === 'state') {
      const now = Date.now();
      if (now - p.lastState < STATE_INTERVAL_MS) return;
      p.lastState = now;

      p.x = Math.max(-23.4, Math.min(23.4, num(m.x, p.x)));
      p.y = Math.max(0, Math.min(12, num(m.y, p.y)));
      p.z = Math.max(-29.4, Math.min(29.4, num(m.z, p.z)));
      p.yaw = num(m.yaw, p.yaw);
      p.pitch = num(m.pitch, p.pitch);
      p.weapon = clean(m.weapon, 24, p.weapon || 'ak47');
      p.model = cleanModel(m.model, p.model || 'chicken');
      p.crouched = m.crouched === true;
      p.godMode = m.godMode === true;
      if (p.godMode) p.hp = 100;

      broadcast(room, {
        type: 'state',
        id: p.id,
        x: p.x, y: p.y, z: p.z,
        yaw: p.yaw, pitch: p.pitch,
        weapon: p.weapon,
        model: p.model,
        hp: p.hp,
        godMode: p.godMode,
        crouched: p.crouched
      }, ws);
      return;
    }

    if (m.type === 'characterModel') {
      p.model = cleanModel(m.model, p.model || 'chicken');
      const state = {
        type: 'state',
        id: p.id,
        x: p.x, y: p.y, z: p.z,
        yaw: p.yaw, pitch: p.pitch,
        weapon: p.weapon,
        model: p.model,
        hp: p.hp,
        godMode: p.godMode,
        crouched: p.crouched
      };
      broadcast(room, state, ws);
      announceRoster(room);
      return;
    }

    if (m.type === 'mode') {
      const mode = String(m.mode || '');
      if (!ALLOWED_MODES.has(mode)) return;
      room.mode = mode;
      broadcast(room, { type: 'mode', mode, by: p.id });
      return;
    }

    if (m.type === 'map') {
      const map = String(m.map || '');
      if (!ALLOWED_MAPS.has(map)) return;
      room.map = map;
      broadcast(room, { type: 'map', map, by: p.id });
      return;
    }

    if (m.type === 'shot') {
      broadcast(room, { type: 'shot', id: p.id, weapon: clean(m.weapon, 24, p.weapon) }, ws);
      return;
    }

    if (m.type === 'weaponAction') {
      const action = String(m.action || '');
      if (!WEAPON_ACTIONS.has(action)) return;
      broadcast(room, {
        type: 'weaponAction',
        id: p.id,
        weapon: clean(m.weapon, 24, p.weapon),
        action
      }, ws);
      return;
    }

    if (m.type === 'hit') {
      const target = room.players.get(String(m.target));
      if (!target || target.id === p.id) return;

      if (target.spawnShieldUntil && Date.now() < target.spawnShieldUntil) {
        send(p.ws, { type: 'notice', message: 'Противник защищён после появления' });
        return;
      }
      if (target.godMode) {
        target.hp = 100;
        return;
      }

      const distance = Math.hypot(target.x - p.x, target.z - p.z);
      if (distance > 85) return;

      const damage = Math.max(1, Math.min(100, num(m.damage, 34)));
      target.hp = Math.max(0, target.hp - damage);
      const killed = target.hp <= 0;
      if (killed) {
        p.kills += 1;
        target.deaths += 1;
      }

      broadcast(room, {
        type: 'hit',
        target: target.id,
        killer: p.id,
        name: p.name,
        hp: target.hp,
        killed
      });
      if (killed) broadcast(room, { type: 'notice', message: p.name + ' выбил игрока ' + target.name });
      return;
    }

    if (m.type === 'respawn') {
      // Spawn position is authoritative: clients cannot pick their own spawn.
      const spawn = randomSpawn();
      p.x = spawn.x;
      p.y = spawn.y;
      p.z = spawn.z;
      p.hp = 100;
      p.yaw = 0;
      p.pitch = 0;
      p.crouched = false;
      p.spawnShieldUntil = Date.now() + SPAWN_SHIELD_MS;

      broadcast(room, {
        type: 'state',
        id: p.id,
        x: p.x, y: p.y, z: p.z,
        yaw: p.yaw, pitch: p.pitch,
        weapon: p.weapon,
        model: p.model || 'chicken',
        hp: p.hp,
        godMode: p.godMode,
        crouched: false,
        spawnShieldMs: SPAWN_SHIELD_MS
      });
      announceRoster(room);
      return;
    }
  });

  ws.on('close', () => leave(ws));
  ws.on('error', () => leave(ws));
});

server.listen(PORT, '0.0.0.0', () => {
  console.log('Poligon friends server v1.1.0 listening on port ' + PORT);
});
