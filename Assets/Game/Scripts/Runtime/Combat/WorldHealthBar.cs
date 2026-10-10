using BattleArena.Pooling;
using UnityEngine;

namespace BattleArena.Combat
{
    /// <summary>
    /// Health bar above a character. Uses the BattleArena/HealthBar shader, which billboards itself,
    /// so this component only pushes the fill value when health changes. Hidden while at full health.
    /// </summary>
    public sealed class WorldHealthBar : MonoBehaviour, IPoolable
    {
        private static readonly int FillId = Shader.PropertyToID("_Fill");

        [SerializeField] private Health health;
        [SerializeField] private Renderer bar;

        private MaterialPropertyBlock _properties;

        /// <summary>Creates the property block used to push the fill value.</summary>
        private void Awake() => _properties = new MaterialPropertyBlock();

        /// <summary>Starts following the health and shows its current state.</summary>
        private void OnEnable()
        {
            health.Changed += Refresh;
            Refresh(health);
        }

        /// <summary>Stops following the health.</summary>
        private void OnDisable() => health.Changed -= Refresh;

        /// <summary>Shows the respawned owner's health (normally full, so hidden).</summary>
        public void OnSpawned() => Refresh(health);

        /// <summary>Hides the bar.</summary>
        public void OnDespawned() => bar.enabled = false;

        /// <summary>
        /// Shows the bar with the current fill while the owner is hurt and alive; hides it otherwise.
        /// </summary>
        private void Refresh(Health source)
        {
            var normalized = source.Normalized;
            var visible = source.IsAlive && normalized < 0.999f;
            bar.enabled = visible;
            if (!visible)
                return;

            // A property block keeps the shared material untouched; the shader reads _Fill per instance.
            _properties.SetFloat(FillId, normalized);
            bar.SetPropertyBlock(_properties);
        }
    }
}
