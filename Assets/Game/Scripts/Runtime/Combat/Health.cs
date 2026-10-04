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
        public bool IsInvulnerable => Time.time < _invulnerableUntil;
        public float InvulnerableRemaining => Mathf.Max(0f, _invulnerableUntil - Time.time);

        /// <summary>Reduces every hit with diminishing returns; see <see cref="ArmorDamageMultiplier"/>.</summary>
        public float Armor { get; set; }

        /// <summary>Armor that halves incoming damage. Twice as much takes a third, and it never reaches immunity.</summary>
        public const float ArmorHalvingPoint = 100f;

        /// <summary>Share of a hit that gets through <paramref name="armor"/>: 100 / (100 + armor), so 100 armor halves damage.</summary>
        public static float ArmorDamageMultiplier(float armor) => ArmorHalvingPoint / (ArmorHalvingPoint + Mathf.Max(0f, armor));

        /// <summary>Raised whenever current or max health changes.</summary>
        public event Action<Health> Changed;
        /// <summary>Raised with the full damage of the hit after armor, even if it exceeds the remaining health.</summary>
        public event Action<Health, DamageResult> Damaged;
        public event Action<Health> Died;
        /// <summary>Raised by <see cref="GrantInvulnerability"/> (e.g. Phase Step), not by the short grace period after a hit.</summary>
        public event Action<float> InvulnerabilityGranted;

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

        public void TakeDamage(float amount, bool isCritical = false)
        {
            if (!IsAlive || amount <= 0f || Time.time < _invulnerableUntil)
                return;

            if (Armor > 0f)
                amount *= ArmorDamageMultiplier(Armor);

            Current = Mathf.Max(0f, Current - amount);
            if (invulnerabilityDuration > 0f)
                _invulnerableUntil = Time.time + invulnerabilityDuration;

            // Report the full hit (not clamped to the remaining health) so damage numbers show what was dealt.
            Damaged?.Invoke(this, new DamageResult(amount, isCritical));
            Changed?.Invoke(this);

            if (Current <= 0f)
                Died?.Invoke(this);
        }

        /// <summary>Ignores all damage for <paramref name="duration"/> seconds.</summary>
        public void GrantInvulnerability(float duration)
        {
            _invulnerableUntil = Mathf.Max(_invulnerableUntil, Time.time + duration);
            InvulnerabilityGranted?.Invoke(duration);
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
