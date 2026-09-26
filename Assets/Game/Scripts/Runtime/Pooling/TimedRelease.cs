using UnityEngine;

namespace BattleArena.Pooling
{
    /// <summary>Returns a pooled object (typically a one-shot VFX) to its pool after a fixed lifetime.</summary>
    public sealed class TimedRelease : MonoBehaviour, IPoolable
    {
        [SerializeField, Min(0.01f)] private float lifetime = 1f;

        public void OnSpawned() => Invoke(nameof(Release), lifetime);

        public void OnDespawned() => CancelInvoke();

        private void Release() => this.Despawn();
    }
}
