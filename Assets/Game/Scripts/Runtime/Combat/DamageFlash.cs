using UnityEngine;

namespace BattleArena.Combat
{
    /// <summary>
    /// Briefly swaps renderer materials to a shared flash material when damaged.
    /// Swapping shared materials keeps renderers SRP Batcher compatible (unlike MaterialPropertyBlocks).
    /// Status looks (a frost rim) add an overlay material that the flash hides and brings back, so the two never fight.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public sealed class DamageFlash : MonoBehaviour
    {
        [SerializeField] private Renderer[] renderers;
        [SerializeField] private Material flashMaterial;
        [SerializeField, Min(0.01f)] private float duration = 0.08f;

        private Health _health;
        // Material lists per renderer, built once so swapping never allocates.
        private Material[][] _plain;
        private Material[][] _overlaid;
        private Material[] _flash;
        private Material _overlay;
        private bool _isFlashing;

        /// <summary>Caches the material lists used for the plain, overlaid and flashing looks.</summary>
        private void Awake()
        {
            _health = GetComponent<Health>();
            _plain = new Material[renderers.Length][];
            _overlaid = new Material[renderers.Length][];
            for (var i = 0; i < renderers.Length; i++)
            {
                _plain[i] = new[] { renderers[i].sharedMaterial };
                _overlaid[i] = new[] { renderers[i].sharedMaterial, null };
            }
            _flash = new[] { flashMaterial };
        }

        /// <summary>Starts listening for hits.</summary>
        private void OnEnable() => _health.Damaged += OnDamaged;

        /// <summary>Stops listening and puts the resting materials back if a flash was running.</summary>
        private void OnDisable()
        {
            _health.Damaged -= OnDamaged;
            Restore();
        }

        /// <summary>
        /// Swaps every renderer to the flash material and schedules the restore; a hit during a flash extends it.
        /// </summary>
        private void OnDamaged(Health health, DamageResult damage)
        {
            if (!isActiveAndEnabled || flashMaterial == null)
                return;

            // Invoke instead of a coroutine: hits are frequent and this avoids a GC allocation per hit.
            CancelInvoke(nameof(Restore));
            if (!_isFlashing)
            {
                _isFlashing = true;
                for (var i = 0; i < renderers.Length; i++)
                    renderers[i].sharedMaterials = _flash;
            }
            Invoke(nameof(Restore), duration);
        }

        /// <summary>
        /// Draws <paramref name="overlay"/> over every renderer on top of its own material; null removes it.
        /// Takes effect at once, or when a running flash ends.
        /// </summary>
        public void SetOverlay(Material overlay)
        {
            _overlay = overlay;
            for (var i = 0; i < _overlaid.Length; i++)
                _overlaid[i][1] = overlay;
            if (!_isFlashing)
                ApplyResting();
        }

        /// <summary>Ends a running flash.</summary>
        private void Restore()
        {
            CancelInvoke(nameof(Restore));
            if (!_isFlashing)
                return;

            _isFlashing = false;
            ApplyResting();
        }

        /// <summary>Shows each renderer's own material, plus the overlay when one is set.</summary>
        private void ApplyResting()
        {
            for (var i = 0; i < renderers.Length; i++)
                renderers[i].sharedMaterials = _overlay != null ? _overlaid[i] : _plain[i];
        }
    }
}
