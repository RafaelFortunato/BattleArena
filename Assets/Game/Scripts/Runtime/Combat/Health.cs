using System;
using UnityEngine;

namespace BattleArena.Combat
{
    /// <summary>Hit points shared by the player, enemies and the boss.</summary>
    [DisallowMultipleComponent]
    public sealed class Health : MonoBehaviour, IDamageable
    {
        [SerializeField, Min(1f)] private float maxHealth = 100f;
        [Tooltip("Seconds of invulnerability after taking a hit. Use for the player only.")]
        [SerializeField, Min(0f)] private float invulnerabilityDuration;

        private float _invulnerableUntil;

        public float Current { get; private set; }
        public float Max => maxHealth;
        public float Normalized => Current / maxHealth;
        public bool IsAlive => Current > 0f;

        /// <summary>Raised whenever current or max health changes.</summary>
        public event Action<Health> Changed;
        /// <summary>Raised with the damage actually applied.</summary>
        public event Action<Health, float> Damaged;
        public event Action<Health> Died;

        private void Awake() => Current = maxHealth;

        /// <summary>Resets to full health with a new maximum. Used when pooled enemies are respawned.</summary>
        public void ResetHealth(float newMax)
        {
            maxHealth = Mathf.Max(1f, newMax);
            Current = maxHealth;
            _invulnerableUntil = 0f;
            Changed?.Invoke(this);
        }

        /// <summary>Raises the maximum and heals by the same amount.</summary>
        public void IncreaseMax(float amount)
        {
            if (amount <= 0f || !IsAlive)
                return;

            maxHealth += amount;
            Current = Mathf.Min(Current + amount, maxHealth);
            Changed?.Invoke(this);
        }

        public void TakeDamage(float amount)
        {
            if (!IsAlive || amount <= 0f || Time.time < _invulnerableUntil)
                return;

            var applied = Mathf.Min(amount, Current);
            Current -= applied;
            if (invulnerabilityDuration > 0f)
                _invulnerableUntil = Time.time + invulnerabilityDuration;

            Damaged?.Invoke(this, applied);
            Changed?.Invoke(this);

            if (Current <= 0f)
                Died?.Invoke(this);
        }

        public void Heal(float amount)
        {
            if (!IsAlive || amount <= 0f || Current >= maxHealth)
                return;

            Current = Mathf.Min(Current + amount, maxHealth);
            Changed?.Invoke(this);
        }
    }
}
