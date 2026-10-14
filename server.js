'use strict';
const http = require('http');
const crypto = require('crypto');
const WebSocket = require('ws');
const PORT = Number(process.env.PORT || 3000);
const MAX_PLAYERS = 6;
const rooms = new Map();
const clients = new Map();
const clean = (s, max = 18) => String(s || '').replace(/[<>\u0000-\u001f]/g, '').trim().slice(0, max) || 'Игрок';
const publicPlayer = p => ({ id:p.id, name:p.name, x:p.x, y:p.y, z:p.z, yaw:p.yaw, pitch:p.pitch, weapon:p.weapon, hp:p.hp, kills:p.kills, deaths:p.deaths });
const send = (ws, obj) => { if (ws.readyState === WebSocket.OPEN) ws.send(JSON.stringify(obj)); };
function roomPlayers(room) { return [...room.players.values()].map(publicPlayer); }
function roomInfo(room) { return { mode:room.mode || 'training', map:room.map || 'dust' }; }
function broadcast(room, obj, except = null) { for (const p of room.players.values()) if (p.ws !== except) send(p.ws, obj); }
function announceRoster(room) { broadcast(room, { type:'players', players:roomPlayers(room) }); }
function leave(ws) {
  const p = clients.get(ws); if (!p) return;
  clients.delete(ws);
  const room = rooms.get(p.room); if (!room) return;
  room.players.delete(p.id);
  if (!room.players.size) rooms.delete(p.room); else announceRoster(room);
}
const server = http.createServer((req,res) => {
  if (req.url === '/health' || req.url === '/') {
    res.writeHead(200, {'content-type':'text/plain; charset=utf-8','access-control-allow-origin':'*'});
    res.end('Poligon friends server is running. WebSocket endpoint is the root URL.'); return;
  }
  res.writeHead(404); res.end('Not found');
});
const wss = new WebSocket.Server({ server, maxPayload: 16 * 1024 });
wss.on('connection', ws => {
  ws.on('message', raw => {
    let m; try { m = JSON.parse(raw.toString()); } catch { return; }
    if (!m || typeof m.type !== 'string') return;
    let p = clients.get(ws);
    if (m.type === 'join') {
      if (p) return;
      const roomCode = clean(m.room, 12).toUpperCase().replace(/[^A-Z0-9_-]/g,'').slice(0,12) || 'KENTY';
      let room = rooms.get(roomCode); if (!room) { room = { players:new Map(), mode:'training', map:'dust' }; rooms.set(roomCode, room); }
      if (room.players.size >= MAX_PLAYERS) { send(ws,{type:'error',message:'Комната заполнена (максимум 6 игроков).'}); ws.close(1008,'room full'); return; }
      const id = crypto.randomUUID();
      p = { id, ws, room:roomCode, name:clean(m.name), x:(Math.random()-.5)*12, y:1.6, z:(Math.random()-.5)*12, yaw:0, pitch:0, weapon:'', hp:100, kills:0, deaths:0, lastState:0 };
      room.players.set(id,p); clients.set(ws,p);
      send(ws,{type:'welcome',id,room:roomCode,players:roomPlayers(room),...roomInfo(room)});
      announceRoster(room); return;
    }
    if (!p) { send(ws,{type:'error',message:'Сначала войди в комнату.'}); return; }
    const room = rooms.get(p.room); if (!room) return;
    if (m.type === 'state') {
      const now=Date.now(); if(now-p.lastState<35)return; p.lastState=now;
      const num=(v, fallback=0)=>Number.isFinite(Number(v))?Number(v):fallback;
      p.x=Math.max(-19.4,Math.min(19.4,num(m.x,p.x))); p.y=Math.max(0,Math.min(12,num(m.y,p.y))); p.z=Math.max(-19.4,Math.min(19.4,num(m.z,p.z)));
      p.yaw=num(m.yaw,p.yaw); p.pitch=num(m.pitch,p.pitch); p.weapon=clean(m.weapon,24);
      broadcast(room,{type:'state',id:p.id,x:p.x,y:p.y,z:p.z,yaw:p.yaw,pitch:p.pitch,weapon:p.weapon,hp:p.hp},ws); return;
    }
    if (m.type === 'mode') {
      const mode=String(m.mode||''); if(!['training','rampage','pigmode'].includes(mode))return;
      room.mode=mode; broadcast(room,{type:'mode',mode,by:p.id}); return;
    }
    if (m.type === 'map') {
      const map=String(m.map||''); if(!['dust','mirage','standoff'].includes(map))return;
      room.map=map; broadcast(room,{type:'map',map,by:p.id}); return;
    }
    if (m.type === 'shot') { broadcast(room,{type:'shot',id:p.id,weapon:clean(m.weapon,24)},ws); return; }
    if (m.type === 'hit') {
      const target=room.players.get(String(m.target)); if(!target || target.id===p.id)return;
      // Lightweight sanity check: only permit hits against players within arena range.
      const d=Math.hypot(target.x-p.x,target.z-p.z); if(d>85)return;
      const damage=Math.max(1,Math.min(100,Number(m.damage)||34)); target.hp=Math.max(0,target.hp-damage);
      let killed=false;
      if(target.hp<=0){killed=true;p.kills++;target.deaths++;}
      broadcast(room,{type:'hit',target:target.id,killer:p.id,name:p.name,hp:target.hp,killed});
      if(killed)broadcast(room,{type:'notice',message:`${p.name} выбил игрока ${target.name}`});
      return;
    }
    if (m.type === 'respawn') { p.hp=100; p.x=(Math.random()-.5)*12; p.y=1.6; p.z=(Math.random()-.5)*12; p.yaw=0; broadcast(room,{type:'state',id:p.id,x:p.x,y:p.y,z:p.z,yaw:p.yaw,pitch:0,hp:p.hp}); announceRoster(room); return; }
  });
  ws.on('close',()=>leave(ws)); ws.on('error',()=>leave(ws));
});
server.listen(PORT,'0.0.0.0',()=>console.log(`Poligon friends server listening on ${PORT}`));
