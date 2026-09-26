using UnityEngine;

namespace BattleArena.Combat
{
    /// <summary>
    /// Damage carried by an attack. Critical hits are rolled per target when the damage is applied,
    /// so an explosion can crit some enemies and not others.
    /// </summary>
    public readonly struct DamageProfile
    {
        public readonly float Amount;
        public readonly float CritChance;
        public readonly float CritMultiplier;

        public DamageProfile(float amount, float critChance = 0f, float critMultiplier = 1f)
        {
            Amount = amount;
            CritChance = critChance;
            CritMultiplier = critMultiplier;
        }

        /// <summary>Damage that never crits, e.g. enemy attacks.</summary>
        public static DamageProfile Flat(float amount) => new(amount);

        public DamageProfile Scaled(float factor) => new(Amount * factor, CritChance, CritMultiplier);

        public float Roll(out bool isCritical)
        {
            isCritical = CritChance > 0f && Random.value < CritChance;
            return isCritical ? Amount * CritMultiplier : Amount;
        }

        /// <summary>Rolls the damage and applies it to <paramref name="target"/>.</summary>
        public void ApplyTo(IDamageable target)
        {
            var amount = Roll(out var isCritical);
            target.TakeDamage(amount, isCritical);
        }
    }
}
