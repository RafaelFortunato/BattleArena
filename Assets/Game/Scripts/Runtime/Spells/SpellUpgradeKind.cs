namespace BattleArena.Spells
{
    /// <summary>Ways a single equipped spell can be improved by mission rewards.</summary>
    public enum SpellUpgradeKind
    {
        /// <summary>Stores one more cast.</summary>
        ExtraCharge,
        /// <summary>Shortens the spell's cooldown.</summary>
        CooldownReduction,
        /// <summary>Raises the spell's share of Attack.</summary>
        Damage,
        /// <summary>Adds projectiles (orbs, meteors) per cast.</summary>
        ExtraProjectiles,
        /// <summary>Makes the spell's effects last longer.</summary>
        Duration,
        /// <summary>Widens the spell's areas.</summary>
        Area,
        /// <summary>Adds chain jumps.</summary>
        ExtraBounces
    }
}
