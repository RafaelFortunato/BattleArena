using BattleArena.Pooling;
using UnityEngine;

namespace BattleArena.Missions
{
    /// <summary>Pooled ground circle marking a mission area, with a fill that shows progress.</summary>
    public sealed class MissionZone : MonoBehaviour, IPoolable
    {
        [Tooltip("Flat marker with a unit diameter, scaled to the zone radius.")]
        [SerializeField] private Transform outline;
        [Tooltip("Flat marker that grows with progress.")]
        [SerializeField] private Transform fill;

        private float _diameter;

        /// <summary>Sizes the circle to <paramref name="radius"/> with an empty fill.</summary>
        public void Show(float radius)
        {
            _diameter = radius * 2f;
            outline.localScale = new Vector3(_diameter, outline.localScale.y, _diameter);
            SetProgress(0f);
        }

        /// <summary>Grows the fill to show progress (0..1).</summary>
        public void SetProgress(float normalized)
        {
            var size = _diameter * Mathf.Clamp01(normalized);
            fill.localScale = new Vector3(size, fill.localScale.y, size);
        }

        /// <summary>Nothing to do: <see cref="Show"/> sets the zone up.</summary>
        public void OnSpawned() { }

        /// <summary>Empties the fill for the next use.</summary>
        public void OnDespawned() => SetProgress(0f);
    }
}
