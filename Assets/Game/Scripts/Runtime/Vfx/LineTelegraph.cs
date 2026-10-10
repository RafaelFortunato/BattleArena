using System.Collections;
using BattleArena.Pooling;
using UnityEngine;

namespace BattleArena.Vfx
{
    /// <summary>Pooled rectangular ground warning that fills along its length (e.g. a charger's dash path).</summary>
    public sealed class LineTelegraph : MonoBehaviour, IPoolable
    {
        [Tooltip("Flat unit cube scaled to the full line.")]
        [SerializeField] private Transform outline;
        [Tooltip("Flat unit cube that grows from the start of the line to its end.")]
        [SerializeField] private Transform fill;
        [SerializeField, Min(0f)] private float heightOffset = 0.05f;

        /// <summary>
        /// Lays the warning along the path and fills it over <paramref name="duration"/> seconds, then despawns.
        /// </summary>
        public void Show(Vector3 start, Vector3 direction, float length, float width, float duration)
        {
            transform.SetPositionAndRotation(start + direction * (length * 0.5f) + Vector3.up * heightOffset,
                Quaternion.LookRotation(direction));
            outline.localScale = new Vector3(width, outline.localScale.y, length);
            StartCoroutine(Fill(length, width, duration));
        }

        /// <summary>Nothing to do: <see cref="Show"/> starts the telegraph.</summary>
        public void OnSpawned() { }

        /// <summary>Stops the fill.</summary>
        public void OnDespawned() => StopAllCoroutines();

        /// <summary>
        /// Grows the fill from the start of the line to its end, then returns the telegraph to its pool.
        /// </summary>
        private IEnumerator Fill(float length, float width, float duration)
        {
            var thickness = fill.localScale.y;
            for (var elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
            {
                var filled = length * (elapsed / duration);
                fill.localScale = new Vector3(width, thickness, filled);
                fill.localPosition = new Vector3(0f, fill.localPosition.y, (filled - length) * 0.5f);
                yield return null;
            }
            this.Despawn();
        }
    }
}
