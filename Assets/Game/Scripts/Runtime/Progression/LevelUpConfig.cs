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

        public int ChoiceCount => choiceCount;
        public IReadOnlyList<StatUpgradeDefinition> StatUpgrades => statUpgrades;
        public IReadOnlyList<SpellDefinition> SpellPool => spellPool;
    }
}
