using UnityEngine;

namespace BattleArena.Combat
{
    /// <summary>
    /// Damage carried by an attack. Critical hits are rolled per target when the damage is applied,
    /// so an explosion can crit some enemies and not others.
    /// </summary>
    public readonly struct DamageProfile
    {
        /// <summary>Base damage of one hit.</summary>
        public readonly float Amount;
        /// <summary>Chance (0..1) that a hit is critical.</summary>
        public readonly float CritChance;
        /// <summary>Damage multiplier applied to a critical hit.</summary>
        public readonly float CritMultiplier;

        /// <summary>Creates a profile; with no crit chance it always deals <paramref name="amount"/>.</summary>
        public DamageProfile(float amount, float critChance = 0f, float critMultiplier = 1f)
        {
            Amount = amount;
            CritChance = critChance;
            CritMultiplier = critMultiplier;
        }

        /// <summary>Damage that never crits, e.g. enemy attacks.</summary>
        public static DamageProfile Flat(float amount) => new(amount);

        /// <summary>The same profile with its base damage multiplied by <paramref name="factor"/>.</summary>
        public DamageProfile Scaled(float factor) => new(Amount * factor, CritChance, CritMultiplier);

        /// <summary>Rolls one hit: the base damage, or the critical damage when the crit roll succeeds.</summary>
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
