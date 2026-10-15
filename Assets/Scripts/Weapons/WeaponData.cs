using UnityEngine;

namespace Poligon.Weapons
{
    [CreateAssetMenu(menuName = "Poligon/Weapon Data", fileName = "NewWeapon")]
    public class WeaponData : ScriptableObject
    {
        public WeaponId id;
        public string displayName = "AK-47";
        public int magSize = 30;
        public int reserveMax = 90;
        public float fireInterval = 0.1f;
        public float reloadTime = 2.4f;
        public bool automatic = true;
        public bool melee;
        public int damageHead = 100;
        public int damageBody = 34;
        public int damageLeg = 26;
        public float baseSpread = 0.05f;
        public float maxSpread = 2.8f;
        public float spreadPerShot = 0.18f;
        public float moveSpreadMult = 4.5f;
        public float range = 120f;
        public GameObject viewModelPrefab;
        public AudioClip fireSound;
        public AudioClip reloadSound;
    }
}
