using BattleArena.Pooling;
using UnityEngine;

namespace BattleArena.Enemies
{
    /// <summary>
    /// Strategy for how an enemy positions itself. Ticked by <see cref="Enemy"/> from the director,
    /// and skipped while an attack holds the enemy.
    /// </summary>
    [RequireComponent(typeof(Enemy))]
    public abstract class EnemyMovement : MonoBehaviour, IPoolable
    {
        protected Enemy Owner { get; private set; }

        protected virtual void Awake() => Owner = GetComponent<Enemy>();

        public abstract void Tick(in EnemyTickContext context);

        public virtual void OnSpawned() { }

        public virtual void OnDespawned() { }

        protected static Vector3 Planar(Vector3 vector)
        {
            vector.y = 0f;
            return vector;
        }
    }
}
