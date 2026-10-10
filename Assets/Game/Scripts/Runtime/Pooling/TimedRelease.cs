using UnityEngine;

namespace BattleArena.Pooling
{
    /// <summary>Returns a pooled object (typically a one-shot VFX) to its pool after a fixed lifetime.</summary>
    public sealed class TimedRelease : MonoBehaviour, IPoolable
    {
        [SerializeField, Min(0.01f)] private float lifetime = 1f;

        /// <summary>Schedules the return to the pool.</summary>
        public void OnSpawned() => Invoke(nameof(Release), lifetime);

        /// <summary>Cancels a pending return.</summary>
        public void OnDespawned() => CancelInvoke();

        /// <summary>Returns the object to its pool.</summary>
        private void Release() => this.Despawn();
    }
}
