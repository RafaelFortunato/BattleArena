using BattleArena.Player;
using UnityEngine;
using UnityEngine.UIElements;

namespace BattleArena.Progression
{
    /// <summary>A level-up card that permanently improves one stat.</summary>
    [CreateAssetMenu(fileName = "StatUpgrade", menuName = "BattleArena/Stat Upgrade")]
    public sealed class StatUpgradeDefinition : ScriptableObject
    {
        [SerializeField] private string displayName = "Upgrade";
        [SerializeField] private StatType stat;
        [Tooltip("Fraction for percentage stats (0.1 = +10%), hit points for Max Health.")]
        [SerializeField] private float amount = 0.1f;
        [SerializeField] private UpgradeRarity rarity;
        [Tooltip("Relative chance of being offered.")]
        [SerializeField, Min(0f)] private float weight = 1f;
        [Tooltip("White line icon for the card.")]
        [SerializeField] private VectorImage icon;

        /// <summary>The card's name.</summary>
        public string DisplayName => displayName;
        /// <summary>The stat the card raises.</summary>
        public StatType Stat => stat;
        /// <summary>
        /// How much the card adds: a fraction for percentage stats (0.1 = +10%), a flat value otherwise.
        /// </summary>
        public float Amount => amount;
        /// <summary>The card's rarity.</summary>
        public UpgradeRarity Rarity => rarity;
        /// <summary>Relative chance of being offered.</summary>
        public float Weight => weight;
        /// <summary>The card's icon.</summary>
        public VectorImage Icon => icon;
    }
}
