using UnityEngine;

namespace Poligon.Combat
{
    public interface IDamageable
    {
        void TakeDamage(int amount, Vector3 point, Vector3 normal);
    }
}
