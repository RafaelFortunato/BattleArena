namespace BattleArena.Spells
{
    /// <summary>
    /// Shared stats that change how a spell behaves, shown on its card and in the pause menu. Attack, crit and
    /// cooldown reduction are left out: every damaging spell uses them.
    /// </summary>
    [System.Flags]
    public enum SpellStats
    {
        /// <summary>No shared stat changes the spell.</summary>
        None = 0,
        /// <summary>The Area stat changes the spell's size.</summary>
        Area = 1,
        /// <summary>The Duration stat makes it last longer.</summary>
        Duration = 2,
        /// <summary>The Projectiles stat adds more of it.</summary>
        Projectiles = 4,
        /// <summary>The Pierce stat lets it hit more targets.</summary>
        Pierce = 8
    }
}
