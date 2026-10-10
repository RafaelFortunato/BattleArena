using System;
using System.Collections.Generic;
using BattleArena.Spells;
using UnityEngine;

namespace BattleArena.Progression
{
    /// <summary>Pools the level-up cards are drawn from.</summary>
    [CreateAssetMenu(fileName = "LevelUpConfig", menuName = "BattleArena/Level Up Config")]
    public sealed class LevelUpConfig : ScriptableObject
    {
        [SerializeField, Range(1, 3)] private int choiceCount = 3;
        [SerializeField] private StatUpgradeDefinition[] statUpgrades = Array.Empty<StatUpgradeDefinition>();
        [SerializeField] private SpellDefinition[] spellPool = Array.Empty<SpellDefinition>();
        [Tooltip("Mission reward cards that improve an equipped spell.")]
        [SerializeField] private SpellUpgradeDefinition[] spellUpgrades = Array.Empty<SpellUpgradeDefinition>();

        /// <summary>Cards shown per choice.</summary>
        public int ChoiceCount => choiceCount;
        /// <summary>Stat cards offered on level-ups and from chests.</summary>
        public IReadOnlyList<StatUpgradeDefinition> StatUpgrades => statUpgrades;
        /// <summary>Spells offered when a spell slot opens.</summary>
        public IReadOnlyList<SpellDefinition> SpellPool => spellPool;
        /// <summary>Spell upgrade cards offered as mission rewards.</summary>
        public IReadOnlyList<SpellUpgradeDefinition> SpellUpgrades => spellUpgrades;
    }
}
