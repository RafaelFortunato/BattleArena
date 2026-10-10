using BattleArena.Pooling;
using UnityEngine;

namespace BattleArena.Enemies.Movement
{
    /// <summary>
    /// Strategy for how an enemy positions itself. Ticked by <see cref="Enemy"/> from the director,
    /// and skipped while an attack holds the enemy.
    /// </summary>
    [RequireComponent(typeof(Enemy))]
    public abstract class EnemyMovement : MonoBehaviour, IPoolable
    {
        /// <summary>The enemy this movement drives.</summary>
        protected Enemy Owner { get; private set; }

        /// <summary>Caches the owning enemy.</summary>
        protected virtual void Awake() => Owner = GetComponent<Enemy>();

        /// <summary>Called on every director tick while the enemy is free to move.</summary>
        public abstract void Tick(in EnemyTickContext context);

        /// <summary>Resets per-life state when the enemy is taken from its pool.</summary>
        public virtual void OnSpawned() { }

        /// <summary>Cleans up when the enemy returns to its pool.</summary>
        public virtual void OnDespawned() { }

        /// <summary><paramref name="vector"/> with its height removed (not normalized).</summary>
        protected static Vector3 Planar(Vector3 vector)
        {
            vector.y = 0f;
            return vector;
        }
    }
}
