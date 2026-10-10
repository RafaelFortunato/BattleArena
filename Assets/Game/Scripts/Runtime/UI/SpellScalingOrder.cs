using BattleArena.Spells;
using UnityEngine;

namespace BattleArena.UI
{
    /// <summary>
    /// The shared stats that change spells, in the order the level-up card and the pause menu list them, with the
    /// icon and short label each one uses.
    /// </summary>
    internal static class SpellScalingOrder
    {
        /// <summary>The shared stats, in display order.</summary>
        public static readonly SpellStats[] Stats = { SpellStats.Area, SpellStats.Duration, SpellStats.Projectiles, SpellStats.Pierce };
        /// <summary>The USS icon class of each stat, in the same order.</summary>
        public static readonly string[] IconClasses = { "icon-stat-area", "icon-stat-duration", "icon-stat-projectiles", "icon-stat-pierce" };
        /// <summary>The short column label of each stat, in the same order.</summary>
        public static readonly string[] ShortNames = { "AREA", "DUR", "PROJ", "PIERCE" };

        /// <summary>Outline and icon colour of a stat the spell does not use.</summary>
        public static readonly Color UnusedColor = new(1f, 1f, 1f, 0.14f);
    }
}
