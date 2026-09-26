using BattleArena.Pooling;
using UnityEngine;

namespace BattleArena.Combat
{
    /// <summary>Damages whatever enters its trigger. Targets are filtered by the layer collision matrix.</summary>
    [RequireComponent(typeof(Collider))]
    public sealed class ContactDamage : MonoBehaviour
    {
        [SerializeField] private TimedRelease hitEffect;

        public DamageProfile Damage { get; set; }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.TryGetComponent(out IDamageable target) || !target.IsAlive)
                return;

            Damage.ApplyTo(target);
            if (hitEffect != null)
                PoolManager.Spawn(hitEffect, other.ClosestPoint(transform.position), Quaternion.identity);
        }
    }
}
