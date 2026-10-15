using UnityEngine;

namespace Poligon.Combat
{
    public class BotTarget : MonoBehaviour, IDamageable
    {
        public int maxHp = 100;
        public int hp = 100;

        Vector3 _spawn;
        Quaternion _spawnRot;
        bool _dead;
        float _dieTimer;
        Vector3 _dieTorque;

        void Start()
        {
            _spawn = transform.position;
            _spawnRot = transform.rotation;
        }

        void Update()
        {
            if (!_dead) return;
            _dieTimer += Time.deltaTime;
            transform.position += Vector3.down * (2.5f * Time.deltaTime);
            transform.Rotate(_dieTorque * Time.deltaTime, Space.World);
            if (_dieTimer > 2.2f)
            {
                // respawn
                _dead = false;
                hp = maxHp;
                transform.position = _spawn;
                transform.rotation = _spawnRot;
                foreach (var r in GetComponentsInChildren<Renderer>())
                    r.enabled = true;
                var col = GetComponent<Collider>();
                if (col) col.enabled = true;
            }
        }

        public void TakeDamage(int amount, Vector3 point, Vector3 normal)
        {
            if (_dead) return;
            hp = Mathf.Max(0, hp - amount);
            // flinch
            transform.position += normal * 0.08f;

            if (hp <= 0)
            {
                _dead = true;
                _dieTimer = 0f;
                _dieTorque = new Vector3(Random.Range(-90f, 90f), Random.Range(-40f, 40f), Random.Range(-90f, 90f));
                var col = GetComponent<Collider>();
                if (col) col.enabled = false;
            }
        }
    }
}
