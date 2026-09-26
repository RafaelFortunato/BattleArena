using System.Collections;
using BattleArena.Pooling;
using UnityEngine;

namespace BattleArena.Vfx
{
    /// <summary>Pooled shockwave that scales from zero to a radius, then returns to its pool.</summary>
    public sealed class ExpandingRing : MonoBehaviour, IPoolable
    {
        [SerializeField, Min(0.01f)] private float duration = 0.35f;
        [SerializeField] private AnimationCurve growth = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        private float _height;

        private void Awake() => _height = transform.localScale.y;

        public void Play(float radius) => StartCoroutine(Expand(radius * 2f));

        public void OnSpawned() => transform.localScale = new Vector3(0f, _height, 0f);

        public void OnDespawned() => StopAllCoroutines();

        private IEnumerator Expand(float diameter)
        {
            for (var elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
            {
                var size = diameter * growth.Evaluate(elapsed / duration);
                transform.localScale = new Vector3(size, _height, size);
                yield return null;
            }
            this.Despawn();
        }
    }
}
