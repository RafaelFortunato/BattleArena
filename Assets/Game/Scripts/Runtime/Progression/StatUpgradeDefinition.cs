using BattleArena.Player;
using UnityEngine;

namespace BattleArena.Progression
{
    public enum UpgradeRarity
    {
        Common,
        Rare,
        Epic
    }

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

        public string DisplayName => displayName;
        public StatType Stat => stat;
        public float Amount => amount;
        public UpgradeRarity Rarity => rarity;
        public float Weight => weight;
    }
}
