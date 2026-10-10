namespace BattleArena.Spells
{
    /// <summary>Caster stats that scale spells (snapshot taken at cast time).</summary>
    public readonly struct SpellModifiers
    {
        /// <summary>The caster's flat Attack; a spell deals a percentage of it.</summary>
        public readonly float Attack;

        /// <summary>This spell's own damage upgrades (1 = none), applied on top of its share of Attack.</summary>
        public readonly float DamageMultiplier;

        /// <summary>Chance for a hit to be critical, 0..1.</summary>
        public readonly float CritChance;

        /// <summary>Damage multiplier of critical hits.</summary>
        public readonly float CritMultiplier;

        /// <summary>Multiplier on the spell's radii.</summary>
        public readonly float AreaMultiplier;

        /// <summary>Multiplier on the spell's durations.</summary>
        public readonly float DurationMultiplier;

        /// <summary>Extra projectiles (or orbs, meteors) per cast.</summary>
        public readonly int ExtraProjectiles;

        /// <summary>Extra targets each projectile passes through.</summary>
        public readonly int ExtraPierce;

        /// <summary>Extra chain jumps (Chain Lightning).</summary>
        public readonly int ExtraBounces;

        /// <summary>Creates a snapshot of the caster's stats.</summary>
        public SpellModifiers(float attack, float critChance, float critMultiplier, float areaMultiplier,
            float durationMultiplier, int extraProjectiles, int extraPierce, int extraBounces = 0, float damageMultiplier = 1f)
        {
            Attack = attack;
            DamageMultiplier = damageMultiplier;
            CritChance = critChance;
            CritMultiplier = critMultiplier;
            AreaMultiplier = areaMultiplier;
            DurationMultiplier = durationMultiplier;
            ExtraProjectiles = extraProjectiles;
            ExtraPierce = extraPierce;
            ExtraBounces = extraBounces;
        }

        /// <summary>Layers one spell slot's upgrades on top of the caster's stats.</summary>
        public SpellModifiers With(SpellUpgradeState upgrades) => new(
            Attack,
            CritChance,
            CritMultiplier,
            AreaMultiplier * (1f + upgrades.Total(SpellUpgradeKind.Area)),
            DurationMultiplier * (1f + upgrades.Total(SpellUpgradeKind.Duration)),
            ExtraProjectiles + upgrades.TotalInt(SpellUpgradeKind.ExtraProjectiles),
            ExtraPierce,
            ExtraBounces + upgrades.TotalInt(SpellUpgradeKind.ExtraBounces),
            DamageMultiplier * (1f + upgrades.Total(SpellUpgradeKind.Damage)));
    }
}
