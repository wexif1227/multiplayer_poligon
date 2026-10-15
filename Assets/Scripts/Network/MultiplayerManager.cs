using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Poligon.Network
{
    public class MultiplayerManager : MonoBehaviour
    {
        public static MultiplayerManager Instance { get; private set; }

        [Header("Connection")]
        public string serverUrl = "ws://localhost:3000";
        public string playerName = "Игрок";
        public string roomCode = "LOBBY";

        [Header("Prefabs")]
        public RemotePlayer remotePrefab;

        public string LocalId { get; private set; }
        public bool Connected { get; private set; }
        public int Kills { get; private set; }
        public int Deaths { get; private set; }
        public int LocalHp { get; private set; } = 100;

        public UnityEvent<string> onStatus = new UnityEvent<string>();
        public UnityEvent onConnected = new UnityEvent();
        public UnityEvent onDisconnected = new UnityEvent();
        public UnityEvent onRosterChanged = new UnityEvent();
        public UnityEvent<int> onLocalDamaged = new UnityEvent<int>();
        public UnityEvent onLocalKilled = new UnityEvent();
        public UnityEvent onLocalKill = new UnityEvent();

        readonly Dictionary<string, RemotePlayer> _remotes = new Dictionary<string, RemotePlayer>();
        WebSocketClient _ws;
        float _sendTimer;
        const float SendInterval = 0.066f;

        Player.PlayerController _local;

        void Awake()
        {
            if (Instance && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void Start()
        {
            _local = FindObjectOfType<Player.PlayerController>();
        }

        public void Connect(string url, string name, string room)
        {
            serverUrl = string.IsNullOrEmpty(url) ? serverUrl : url;
            playerName = string.IsNullOrEmpty(name) ? playerName : name;
            roomCode = string.IsNullOrEmpty(room) ? "LOBBY" : room.ToUpperInvariant();

            _ws?.Dispose();
            _ws = new WebSocketClient();
            _ws.OnOpen += () =>
            {
                Connected = true;
                onStatus?.Invoke("Подключено, вход в комнату…");
                Send(new MpOutgoing { type = "join", name = playerName, room = roomCode });
            };
            _ws.OnClose += () =>
            {
                Connected = false;
                LocalId = null;
                ClearRemotes();
                onDisconnected?.Invoke();
                onStatus?.Invoke("Соединение закрыто");
            };
            _ws.OnError += err => onStatus?.Invoke("Ошибка: " + err);

            onStatus?.Invoke("Подключение к " + serverUrl);
            _ = _ws.ConnectAsync(serverUrl);
        }

        public void Disconnect()
        {
            _ws?.Disconnect();
            Connected = false;
            ClearRemotes();
        }

        void Update()
        {
            if (_ws == null) return;
            while (_ws.TryDequeue(out var raw)) HandleMessage(raw);

            if (!Connected || _local == null) return;
            _sendTimer += Time.deltaTime;
            if (_sendTimer >= SendInterval)
            {
                _sendTimer = 0f;
                var t = _local.transform;
                var cam = _local.PlayerCamera != null ? _local.PlayerCamera.transform : t;
                Send(new MpOutgoing
                {
                    type = "state",
                    x = t.position.x,
                    y = t.position.y,
                    z = t.position.z,
                    yaw = cam.eulerAngles.y * Mathf.Deg2Rad,
                    pitch = cam.eulerAngles.x * Mathf.Deg2Rad,
                    weapon = _local.CurrentWeaponId.ToString(),
                    godMode = _local.GodMode
                });
            }

            foreach (var r in _remotes.Values)
                r.Tick(Time.deltaTime);
        }

        void HandleMessage(string raw)
        {
            MpMessage m;
            try { m = JsonUtility.FromJson<MpMessage>(raw); }
            catch { return; }
            if (m == null || string.IsNullOrEmpty(m.type)) return;

            switch (m.type)
            {
                case "welcome":
                    LocalId = m.id;
                    roomCode = m.room;
                    Connected = true;
                    onStatus?.Invoke("В комнате " + m.room);
                    onConnected?.Invoke();
                    if (m.players != null)
                        foreach (var p in m.players)
                            if (p.id != LocalId) UpsertRemote(p);
                    onRosterChanged?.Invoke();
                    break;

                case "players":
                    SyncRoster(m.players);
                    onRosterChanged?.Invoke();
                    break;

                case "state":
                    if (m.id == LocalId) break;
                    UpsertRemote(new MpPlayer
                    {
                        id = m.id, x = m.x, y = m.y, z = m.z,
                        yaw = m.yaw, pitch = m.pitch, weapon = m.weapon, hp = m.hp
                    });
                    break;

                case "shot":
                    if (_remotes.TryGetValue(m.id, out var shooter))
                        shooter.PlayShot(m.weapon);
                    break;

                case "hit":
                    if (m.target == LocalId)
                    {
                        if (_local != null && _local.GodMode)
                        {
                            LocalHp = 100;
                        }
                        else
                        {
                            LocalHp = m.hp;
                            onLocalDamaged?.Invoke(LocalHp);
                            if (LocalHp <= 0)
                            {
                                Deaths++;
                                LocalHp = 100;
                                onLocalKilled?.Invoke();
                                if (_local != null) _local.Respawn();
                                Send(new MpOutgoing { type = "respawn" });
                            }
                        }
                    }
                    if (m.killer == LocalId && m.hp <= 0)
                    {
                        Kills++;
                        onLocalKill?.Invoke();
                    }
                    if (_remotes.TryGetValue(m.target, out var victim))
                        victim.SetHp(m.hp);
                    onRosterChanged?.Invoke();
                    break;

                case "error":
                    onStatus?.Invoke(m.message ?? "Ошибка сервера");
                    break;

                case "notice":
                    onStatus?.Invoke(m.message ?? "");
                    break;

                case "mode":
                case "map":
                    // optional: react in GameManager
                    break;
            }
        }

        void SyncRoster(MpPlayer[] list)
        {
            if (list == null) return;
            var seen = new HashSet<string>();
            foreach (var p in list)
            {
                if (p.id == LocalId) continue;
                seen.Add(p.id);
                UpsertRemote(p);
            }
            var toRemove = new List<string>();
            foreach (var id in _remotes.Keys)
                if (!seen.Contains(id)) toRemove.Add(id);
            foreach (var id in toRemove)
            {
                if (_remotes.TryGetValue(id, out var r) && r) Destroy(r.gameObject);
                _remotes.Remove(id);
            }
        }

        void UpsertRemote(MpPlayer p)
        {
            if (p == null || string.IsNullOrEmpty(p.id)) return;
            if (!_remotes.TryGetValue(p.id, out var r) || r == null)
            {
                if (remotePrefab == null) return;
                r = Instantiate(remotePrefab);
                r.name = "Remote_" + p.id;
                _remotes[p.id] = r;
            }
            r.ApplyState(p);
        }

        void ClearRemotes()
        {
            foreach (var r in _remotes.Values)
                if (r) Destroy(r.gameObject);
            _remotes.Clear();
        }

        public void SendShot(WeaponId weapon)
        {
            Send(new MpOutgoing { type = "shot", weapon = weapon.ToString() });
        }

        public void SendHit(string targetId, int damage)
        {
            Send(new MpOutgoing { type = "hit", target = targetId, damage = damage });
        }

        public IEnumerable<RemotePlayer> Remotes => _remotes.Values;

        public RemotePlayer FindRemoteAlongRay(Ray ray, float maxDist, float hitRadius = 0.48f)
        {
            RemotePlayer best = null;
            float bestDist = maxDist;
            foreach (var r in _remotes.Values)
            {
                if (r == null) continue;
                var center = r.AimPoint;
                var to = center - ray.origin;
                float along = Vector3.Dot(to, ray.direction);
                if (along <= 0f || along >= bestDist) continue;
                var closest = ray.origin + ray.direction * along;
                if ((closest - center).sqrMagnitude > hitRadius * hitRadius) continue;
                best = r;
                bestDist = along;
            }
            return best;
        }

        void Send(MpOutgoing o)
        {
            if (_ws == null || !Connected) return;
            _ws.Send(JsonUtility.ToJson(o));
        }

        void OnDestroy()
        {
            _ws?.Dispose();
            if (Instance == this) Instance = null;
        }
    }
}
