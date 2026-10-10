using BattleArena.Combat;
using BattleArena.Pooling;
using UnityEngine;

namespace BattleArena.Enemies.Attacks
{
    /// <summary>Strategy for one way an enemy hurts the player. An enemy may have several.</summary>
    [RequireComponent(typeof(Enemy))]
    public abstract class EnemyAttack : MonoBehaviour, IPoolable
    {
        /// <summary>The enemy this attack belongs to.</summary>
        protected Enemy Owner { get; private set; }

        /// <summary>Caches the owning enemy.</summary>
        protected virtual void Awake() => Owner = GetComponent<Enemy>();

        /// <summary>Called on every director tick while the enemy is alive.</summary>
        public abstract void Tick(in EnemyTickContext context);

        /// <summary>Resets per-life state when the enemy is taken from its pool.</summary>
        public virtual void OnSpawned() { }

        /// <summary>Cleans up when the enemy returns to its pool.</summary>
        public virtual void OnDespawned() { }

        /// <summary>Squared distance from the enemy to <paramref name="position"/>, ignoring height.</summary>
        protected float PlanarSqrDistanceTo(Vector3 position)
        {
            var offset = position - transform.position;
            offset.y = 0f;
            return offset.sqrMagnitude;
        }

        /// <summary>Planar squared distance from a point to a segment; used to catch fast movers between ticks.</summary>
        protected static float SqrDistanceToSegment(Vector3 point, Vector3 a, Vector3 b)
        {
            point.y = a.y = b.y = 0f;
            var segment = b - a;
            var lengthSqr = segment.sqrMagnitude;
            var t = lengthSqr > 0.0001f ? Mathf.Clamp01(Vector3.Dot(point - a, segment) / lengthSqr) : 0f;
            return (a + segment * t - point).sqrMagnitude;
        }

        /// <summary>Damage scaled by the enemy's difficulty multiplier. Enemy hits never crit.</summary>
        protected DamageProfile ScaledDamage(float baseDamage) => DamageProfile.Flat(baseDamage * Owner.DamageMultiplier);
    }
}
