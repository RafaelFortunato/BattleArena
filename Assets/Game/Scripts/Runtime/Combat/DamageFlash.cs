using UnityEngine;

namespace BattleArena.Combat
{
    /// <summary>
    /// Briefly swaps renderer materials to a shared flash material when damaged.
    /// Swapping shared materials keeps renderers SRP Batcher compatible (unlike MaterialPropertyBlocks).
    /// </summary>
    [RequireComponent(typeof(Health))]
    public sealed class DamageFlash : MonoBehaviour
    {
        [SerializeField] private Renderer[] renderers;
        [SerializeField] private Material flashMaterial;
        [SerializeField, Min(0.01f)] private float duration = 0.08f;

        private Health _health;
        private Material[] _originalMaterials;
        private bool _isFlashing;

        private void Awake()
        {
            _health = GetComponent<Health>();
            _originalMaterials = new Material[renderers.Length];
            for (var i = 0; i < renderers.Length; i++)
                _originalMaterials[i] = renderers[i].sharedMaterial;
        }

        private void OnEnable() => _health.Damaged += OnDamaged;

        private void OnDisable()
        {
            _health.Damaged -= OnDamaged;
            Restore();
        }

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
                    renderers[i].sharedMaterial = flashMaterial;
            }
            Invoke(nameof(Restore), duration);
        }

        private void Restore()
        {
            CancelInvoke(nameof(Restore));
            if (!_isFlashing)
                return;

            _isFlashing = false;
            for (var i = 0; i < renderers.Length; i++)
                renderers[i].sharedMaterial = _originalMaterials[i];
        }
    }
}
