using UnityEngine;

namespace Poligon.Network
{
    public class RemotePlayer : MonoBehaviour
    {
        public TextMesh nameLabel;
        public string NetId { get; private set; }
        public string DisplayName { get; private set; } = "Player";
        public int Hp { get; private set; } = 100;
        public string Weapon { get; private set; } = "ak47";

        Vector3 _targetPos;
        float _targetYaw;

        public Vector3 AimPoint => transform.position + Vector3.up * 1.05f;

        public void ApplyState(MpPlayer p)
        {
            NetId = p.id;
            if (!string.IsNullOrEmpty(p.name))
            {
                DisplayName = p.name;
                if (nameLabel) nameLabel.text = p.name;
            }
            _targetPos = new Vector3(p.x, p.y, p.z);
            _targetYaw = p.yaw * Mathf.Rad2Deg;
            if (p.hp > 0) Hp = p.hp;
            if (!string.IsNullOrEmpty(p.weapon)) Weapon = p.weapon;
        }

        public void SetHp(int hp) => Hp = hp;
        public void PlayShot(string weapon) { Weapon = weapon ?? Weapon; }

        public void Tick(float dt)
        {
            transform.position = Vector3.Lerp(transform.position, _targetPos, 1f - Mathf.Exp(-14f * dt));
            var e = transform.eulerAngles;
            e.y = Mathf.LerpAngle(e.y, _targetYaw, 1f - Mathf.Exp(-14f * dt));
            transform.eulerAngles = e;
        }
    }
}
