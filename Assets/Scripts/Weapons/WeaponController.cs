using System.Collections.Generic;
using UnityEngine;
using Poligon.Player;
using Poligon.Network;
using Poligon.Combat;

namespace Poligon.Weapons
{
    public class WeaponController : MonoBehaviour
    {
        public List<WeaponData> arsenal = new List<WeaponData>();
        public Transform muzzle;
        public LayerMask hitMask = ~0;
        public Light muzzleFlashLight;
        public AudioSource audioSource;

        public WeaponId CurrentId { get; private set; } = WeaponId.ak47;
        public int Mag { get; private set; }
        public int Reserve { get; private set; }
        public bool Reloading { get; private set; }
        public float SpreadVisual { get; private set; }

        WeaponData _data;
        float _nextFire, _reloadEnd, _spread;
        GameObject _viewModel;
        Vector3 _recoilPos, _recoilRot;
        Vector3 _swayRot;
        Vector3 _baseMuzzlePos;
        float _inspect;

        readonly Dictionary<WeaponId, (int mag, int reserve)> _ammo = new Dictionary<WeaponId, (int, int)>();

        void Start()
        {
            if (muzzle != null) _baseMuzzlePos = muzzle.localPosition;
            if (arsenal == null) arsenal = new List<WeaponData>();
            if (arsenal.Count == 0)
            {
                var loaded = Resources.LoadAll<WeaponData>("Weapons");
                if (loaded != null && loaded.Length > 0) arsenal.AddRange(loaded);
            }
            if (arsenal.Count == 0)
            {
                arsenal.Add(Mk(WeaponId.ak47, "AK-47", 30, 90, 0.1f, true, 36));
                arsenal.Add(Mk(WeaponId.m4a1s, "M4A1-S | PRINTSTREAM", 25, 75, 0.085f, true, 33));
                arsenal.Add(Mk(WeaponId.awp, "AWP | PRINTSTREAM", 10, 30, 1.2f, false, 115));
                arsenal.Add(Mk(WeaponId.glock, "Glock-18", 20, 120, 0.11f, false, 28));
                arsenal.Add(Mk(WeaponId.knife, "КЕРАМБИТ | EMERALD", 1, 0, 0.5f, false, 65, true));
            }
            foreach (var w in arsenal)
                if (w != null) _ammo[w.id] = (w.magSize, w.reserveMax);
            if (arsenal.Count > 0) Equip(arsenal[0].id);
        }

        static WeaponData Mk(WeaponId id, string n, int mag, int res, float iv, bool auto, int dmg, bool melee = false)
        {
            var w = ScriptableObject.CreateInstance<WeaponData>();
            w.id = id; w.displayName = n; w.magSize = mag; w.reserveMax = res;
            w.fireInterval = iv; w.reloadTime = melee ? 0.1f : 2.15f;
            w.automatic = auto; w.melee = melee;
            w.damageHead = 100; w.damageBody = dmg; w.damageLeg = dmg / 2;
            w.baseSpread = melee ? 0 : 0.035f; w.maxSpread = 2.6f; w.spreadPerShot = 0.15f;
            w.moveSpreadMult = 4.2f; w.range = melee ? 2.6f : 150f;
            return w;
        }

        public void Tick(PlayerController player)
        {
            for (int i = 0; i < 9 && i < arsenal.Count; i++)
                if (Input.GetKeyDown(KeyCode.Alpha1 + i)) Equip(arsenal[i].id);

            AnimateViewmodel(player);

            if (_data == null) return;
            if (Reloading && Time.time >= _reloadEnd) FinishReload();
            if (Input.GetKeyDown(KeyCode.R) && !Reloading && !_data.melee && Mag < _data.magSize && Reserve > 0)
                StartReload();

            bool fire = _data.automatic ? Input.GetButton("Fire1") : Input.GetButtonDown("Fire1");
            if (fire && !Reloading && Time.time >= _nextFire && (_data.melee || Mag > 0))
                Fire(player);

            _spread = Mathf.MoveTowards(_spread, _data.baseSpread, Time.deltaTime * 7f);
            SpreadVisual = _spread;
        }

        void AnimateViewmodel(PlayerController player)
        {
            if (muzzle == null) return;

            // Mouse look sway
            float mx = Input.GetAxis("Mouse X");
            float my = Input.GetAxis("Mouse Y");
            _swayRot.x = Mathf.Lerp(_swayRot.x, my * 1.2f, Time.deltaTime * 6f);
            _swayRot.y = Mathf.Lerp(_swayRot.y, -mx * 1.2f, Time.deltaTime * 6f);
            _swayRot.z = Mathf.Lerp(_swayRot.z, -mx * 0.6f, Time.deltaTime * 6f);

            // Walk bob on gun
            float bob = player != null ? player.MoveSpeed01 : 0f;
            float t = Time.time * 9f;
            var walkPos = new Vector3(Mathf.Cos(t) * 0.012f * bob, Mathf.Sin(t * 2f) * 0.01f * bob, 0f);

            // Recoil recovery
            _recoilPos = Vector3.Lerp(_recoilPos, Vector3.zero, Time.deltaTime * 7f);
            _recoilRot = Vector3.Lerp(_recoilRot, Vector3.zero, Time.deltaTime * 7f);

            // Reload dip
            float reloadDip = Reloading ? Mathf.Sin((Time.time - (_reloadEnd - (_data != null ? _data.reloadTime : 2f))) / Mathf.Max(0.01f, _data != null ? _data.reloadTime : 2f) * Mathf.PI) * 0.15f : 0f;

            muzzle.localPosition = _baseMuzzlePos + _recoilPos + walkPos + Vector3.down * reloadDip;
            muzzle.localRotation = Quaternion.Euler(_recoilRot + _swayRot);
        }

        public void Equip(WeaponId id)
        {
            if (_data != null) _ammo[_data.id] = (Mag, Reserve);
            WeaponData next = null;
            foreach (var w in arsenal)
                if (w != null && w.id == id) { next = w; break; }
            if (next == null) return;

            _data = next;
            CurrentId = id;
            if (_ammo.TryGetValue(id, out var a)) { Mag = a.mag; Reserve = a.reserve; }
            else { Mag = _data.magSize; Reserve = _data.reserveMax; }
            Reloading = false;
            _recoilPos = new Vector3(0, -0.08f, 0.05f); // draw animation

            if (muzzle != null)
            {
                foreach (Transform c in muzzle)
                    if (c.name.StartsWith("VM_") || c.name == "ViewGun") Destroy(c.gameObject);
                _viewModel = ViewModelBuilder.Build(id, muzzle);
            }
        }

        void Fire(PlayerController player)
        {
            _nextFire = Time.time + _data.fireInterval;
            if (!_data.melee)
            {
                Mag--;
                _spread = Mathf.Min(_data.maxSpread, _spread + _data.spreadPerShot);
            }

            // Kick
            float kick = CurrentId == WeaponId.awp ? 8f : (_data.melee ? 3f : 2.2f);
            _recoilRot += new Vector3(-kick, Random.Range(-1f, 1f), Random.Range(-0.5f, 0.5f));
            _recoilPos += new Vector3(0f, 0f, -0.04f * kick * 0.25f);

            if (muzzleFlashLight)
            {
                muzzleFlashLight.enabled = true;
                muzzleFlashLight.intensity = CurrentId == WeaponId.awp ? 5f : 2.8f;
                CancelInvoke(nameof(HideMuzzle));
                Invoke(nameof(HideMuzzle), 0.04f);
            }

            var mp = MultiplayerManager.Instance;
            if (mp != null && mp.Connected) mp.SendShot(CurrentId);

            var cam = player.PlayerCamera != null ? player.PlayerCamera : Camera.main;
            if (cam == null) return;

            float spreadMul = 1f;
            if (player.IsMoving) spreadMul *= _data.moveSpreadMult;
            if (!player.IsGrounded) spreadMul *= 2.2f;
            float rad = _spread * spreadMul * Mathf.Deg2Rad;
            var dir = cam.transform.forward;
            dir = Quaternion.Euler(Random.Range(-rad, rad) * Mathf.Rad2Deg, Random.Range(-rad, rad) * Mathf.Rad2Deg, 0) * dir;
            var ray = new Ray(cam.transform.position, dir);

            if (mp != null && mp.Connected)
            {
                var remote = mp.FindRemoteAlongRay(ray, _data.range);
                if (remote != null)
                {
                    int dmg = CurrentId == WeaponId.awp ? 100 : _data.damageBody;
                    if (_data.melee) dmg = 55;
                    mp.SendHit(remote.NetId, dmg);
                }
            }

            if (Physics.Raycast(ray, out var hit, _data.range, hitMask))
            {
                var dmgable = hit.collider.GetComponentInParent<IDamageable>();
                if (dmgable != null)
                    dmgable.TakeDamage(_data.damageBody, hit.point, hit.normal);

                SpawnImpact(hit.point, hit.normal);
            }
        }

        void SpawnImpact(Vector3 pos, Vector3 normal)
        {
            var mark = GameObject.CreatePrimitive(PrimitiveType.Quad);
            Object.Destroy(mark.GetComponent<Collider>());
            mark.name = "Impact";
            mark.transform.position = pos + normal * 0.01f;
            mark.transform.rotation = Quaternion.LookRotation(-normal);
            mark.transform.localScale = Vector3.one * 0.12f;
            var sh = Shader.Find("Unlit/Color") ?? Shader.Find("Standard");
            var mat = new Material(sh);
            mat.color = new Color(1f, 0.55f, 0.15f, 1f);
            mark.GetComponent<Renderer>().sharedMaterial = mat;
            Object.Destroy(mark, 3f);

            // spark burst
            for (int i = 0; i < 4; i++)
            {
                var s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                Object.Destroy(s.GetComponent<Collider>());
                s.transform.position = pos;
                s.transform.localScale = Vector3.one * Random.Range(0.02f, 0.05f);
                var rb = s.AddComponent<Rigidbody>();
                rb.mass = 0.01f;
                rb.velocity = (normal + Random.insideUnitSphere) * Random.Range(2f, 5f);
                var rs = s.GetComponent<Renderer>();
                var sm = new Material(sh);
                sm.color = new Color(1f, Random.Range(0.5f, 1f), 0.2f);
                rs.sharedMaterial = sm;
                Object.Destroy(s, 0.6f);
            }
        }

        void HideMuzzle() { if (muzzleFlashLight) muzzleFlashLight.enabled = false; }

        void StartReload()
        {
            Reloading = true;
            _reloadEnd = Time.time + _data.reloadTime;
        }

        void FinishReload()
        {
            Reloading = false;
            int need = _data.magSize - Mag;
            int take = Mathf.Min(need, Reserve);
            Mag += take; Reserve -= take;
        }

        public string HudName => _data != null ? _data.displayName : "";
        public bool IsMelee => _data != null && _data.melee;
        public bool IsScoped => CurrentId == WeaponId.awp && Input.GetButton("Fire2");
    }
}
