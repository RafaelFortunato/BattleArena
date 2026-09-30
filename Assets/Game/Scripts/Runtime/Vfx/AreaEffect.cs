using BattleArena.Pooling;
using UnityEngine;

namespace BattleArena.Vfx
{
    /// <summary>
    /// Pooled one-shot effect authored for a radius of 1 (particle systems use hierarchy scaling), scaled to the
    /// area of the spell that spawned it, then returned to its pool.
    /// </summary>
    public sealed class AreaEffect : MonoBehaviour, IPoolable
    {
        [SerializeField, Min(0.05f)] private float lifetime = 1.2f;

        public void Play(float radius) => transform.localScale = Vector3.one * radius;

        public void OnSpawned()
        {
            transform.localScale = Vector3.one;
            Invoke(nameof(Release), lifetime);
        }

        public void OnDespawned() => CancelInvoke();

        private void Release() => this.Despawn();
    }
}
