using UnityEngine;
using Poligon.Player;
using Poligon.Weapons;
using Poligon.Network;
using Poligon.Combat;

namespace Poligon.UI
{
    public class HudController : MonoBehaviour
    {
        PlayerController _player;
        WeaponController _weapons;
        string _status = "";
        bool _showMp;
        string _server = "ws://localhost:3000", _name = "Игрок", _room = "LOBBY";
        GUIStyle _title, _label, _ammo;
        bool _ready;
        float _hitFlash;

        void Start()
        {
            _player = FindObjectOfType<PlayerController>();
            _weapons = FindObjectOfType<WeaponController>();
            var mp = MultiplayerManager.Instance;
            if (mp != null)
            {
                mp.onStatus.AddListener(s => _status = s);
                mp.onLocalKill.AddListener(() => _hitFlash = 0.35f);
            }
        }

        void Update()
        {
            if (_player == null) _player = FindObjectOfType<PlayerController>();
            if (_weapons == null) _weapons = FindObjectOfType<WeaponController>();
            if (Input.GetKeyDown(KeyCode.Tab)) _showMp = !_showMp;
            _hitFlash = Mathf.Max(0f, _hitFlash - Time.deltaTime);
        }

        void Ensure()
        {
            if (_ready) return;
            _title = new GUIStyle(GUI.skin.label) { fontSize = 24, fontStyle = FontStyle.Bold, normal = { textColor = new Color(0.95f, 0.85f, 0.4f) } };
            _label = new GUIStyle(GUI.skin.label) { fontSize = 15, normal = { textColor = new Color(0.9f, 0.92f, 0.95f) } };
            _ammo = new GUIStyle(GUI.skin.label) { fontSize = 36, fontStyle = FontStyle.Bold, alignment = TextAnchor.LowerRight, normal = { textColor = Color.white } };
            _ready = true;
        }

        void OnGUI()
        {
            Ensure();
            float cx = Screen.width * 0.5f, cy = Screen.height * 0.5f;
            float spread = _weapons != null ? 6f + _weapons.SpreadVisual * 12f : 8f;
            if (_weapons != null && _weapons.IsScoped)
            {
                GUI.color = new Color(0, 0, 0, 0.55f);
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
                GUI.color = Color.white;
                DrawRing(cx, cy, Screen.height * 0.28f);
                GUI.DrawTexture(new Rect(cx - 1, 0, 2, Screen.height), Texture2D.whiteTexture);
                GUI.DrawTexture(new Rect(0, cy - 1, Screen.width, 2), Texture2D.whiteTexture);
            }
            else
            {
                Color cc = _hitFlash > 0 ? new Color(1f, 0.45f, 0.2f) : new Color(1f, 1f, 1f, 0.9f);
                GUI.color = cc;
                Seg(cx - spread - 11, cy, 11, 2);
                Seg(cx + spread, cy, 11, 2);
                Seg(cx, cy - spread - 11, 2, 11);
                Seg(cx, cy + spread, 2, 11);
                Seg(cx - 1.5f, cy - 1.5f, 3, 3);
                GUI.color = Color.white;
            }

            GUI.Label(new Rect(18, 14, 400, 28), "POLIGON", _title);
            if (_player != null)
            {
                GUI.Label(new Rect(18, 46, 200, 22), "HP  " + _player.Health, _label);
                GUI.Box(new Rect(18, 70, 170, 14), "");
                GUI.color = new Color(1f, 0.35f, 0.3f);
                GUI.DrawTexture(new Rect(18, 70, 170 * (_player.Health / 100f), 14), Texture2D.whiteTexture);
                GUI.color = Color.white;
            }
            if (_weapons != null)
            {
                GUI.Label(new Rect(18, 92, 500, 22), _weapons.HudName, _label);
                string a = _weapons.IsMelee ? "∞" : (_weapons.Reloading ? "RELOAD" : _weapons.Mag + " / " + _weapons.Reserve);
                if (!_weapons.IsMelee && _weapons.Mag <= 5) _ammo.normal.textColor = new Color(1f, 0.4f, 0.35f);
                else _ammo.normal.textColor = Color.white;
                GUI.Label(new Rect(Screen.width - 240, Screen.height - 72, 220, 44), a, _ammo);
            }
            var mp = MultiplayerManager.Instance;
            if (mp != null)
                GUI.Label(new Rect(18, 116, 400, 20), "K/D " + mp.Kills + "/" + mp.Deaths + (mp.Connected ? "  ONLINE" : ""), _label);

            int by = 145;
            foreach (var b in Object.FindObjectsOfType<BotTarget>())
            {
                if (!b.gameObject.activeInHierarchy || b.hp <= 0) continue;
                GUI.Label(new Rect(18, by, 220, 18), b.name + "  " + b.hp, _label);
                by += 17;
                if (by > 250) break;
            }

            GUI.Label(new Rect(18, Screen.height - 24, 760, 20),
                "WASD  Shift  Mouse  LMB  RMB=scope(AWP)  R  1-5  Tab  Esc", _label);

            if (_showMp)
            {
                float w = 400, h = 240, x = (Screen.width - w) / 2, y = (Screen.height - h) / 2;
                GUI.Box(new Rect(x, y, w, h), "MULTIPLAYER");
                GUI.Label(new Rect(x + 20, y + 40, 80, 22), "Server");
                _server = GUI.TextField(new Rect(x + 100, y + 40, 270, 22), _server);
                GUI.Label(new Rect(x + 20, y + 72, 80, 22), "Name");
                _name = GUI.TextField(new Rect(x + 100, y + 72, 270, 22), _name);
                GUI.Label(new Rect(x + 20, y + 104, 80, 22), "Room");
                _room = GUI.TextField(new Rect(x + 100, y + 104, 270, 22), _room);
                if (GUI.Button(new Rect(x + 20, y + 150, 170, 34), "Connect"))
                    MultiplayerManager.Instance?.Connect(_server, _name, _room);
                if (GUI.Button(new Rect(x + 210, y + 150, 170, 34), "Disconnect"))
                    MultiplayerManager.Instance?.Disconnect();
            }
        }

        void Seg(float x, float y, float w, float h) =>
            GUI.DrawTexture(new Rect(x, y - h * 0.5f + 1, w, h), Texture2D.whiteTexture);

        void DrawRing(float cx, float cy, float r)
        {
            // approximate ring with box hole using 4 rects is heavy; just corner markers
            GUI.color = new Color(0, 0, 0, 0.85f);
            GUI.DrawTexture(new Rect(0, 0, cx - r, Screen.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(cx + r, 0, Screen.width - (cx + r), Screen.height), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(cx - r, 0, r * 2, cy - r), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(cx - r, cy + r, r * 2, Screen.height - (cy + r)), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
    }
}
