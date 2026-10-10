namespace BattleArena.Player
{
    /// <summary>The character stats that level-up cards can raise. Values index <see cref="PlayerStats"/> tables.</summary>
    public enum StatType
    {
        /// <summary>Movement speed bonus.</summary>
        MoveSpeed,
        /// <summary>Maximum health.</summary>
        MaxHealth,
        /// <summary>Flat attack: basic attacks deal 100% of it, spells a percentage.</summary>
        Attack,
        /// <summary>Chance for a hit to be critical.</summary>
        CritChance,
        /// <summary>Damage multiplier of critical hits.</summary>
        CritDamage,
        /// <summary>How often basic attacks fire. Spells are unaffected (see <see cref="CooldownReduction"/>).</summary>
        AttackSpeed,
        /// <summary>Size of spell areas.</summary>
        Area,
        /// <summary>How long lasting spell effects (zones, orbs, slows) persist.</summary>
        Duration,
        /// <summary>Extra projectiles per shot.</summary>
        Multishot,
        /// <summary>Extra targets a projectile passes through.</summary>
        Pierce,
        /// <summary>Radius in which experience gems are pulled in.</summary>
        PickupRadius,
        /// <summary>Damage reduction with diminishing returns (see <see cref="BattleArena.Combat.Health.Armor"/>).</summary>
        Armor,
        /// <summary>Health restored per second.</summary>
        Regeneration,
        /// <summary>Bonus to experience gained.</summary>
        ExperienceGain,
        /// <summary>Raises the odds of rare and epic cards.</summary>
        Luck,
        /// <summary>Share taken off every spell cooldown. Basic attacks are unaffected (see <see cref="AttackSpeed"/>).</summary>
        CooldownReduction
    }
}
