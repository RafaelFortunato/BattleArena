using System.Globalization;
using UnityEngine;

namespace BattleArena.Spells
{
    /// <summary>A spell upgrade card (mission reward). Applies to one equipped spell that supports its kind.</summary>
    [CreateAssetMenu(fileName = "SpellUpgrade", menuName = "BattleArena/Spell Upgrade")]
    public sealed class SpellUpgradeDefinition : ScriptableObject
    {
        [SerializeField] private string displayName = "Upgrade";
        [SerializeField] private SpellUpgradeKind kind;
        [Tooltip("Fraction for percentage upgrades (0.2 = 20%), count for charges, projectiles and bounces.")]
        [SerializeField] private float amount = 0.2f;
        [SerializeField, Min(1)] private int maxRank = 3;
        [SerializeField, Min(0f)] private float weight = 1f;

        public string DisplayName => displayName;
        public SpellUpgradeKind Kind => kind;
        public float Amount => amount;
        public int MaxRank => maxRank;
        public float Weight => weight;

        /// <summary>Card text for this upgrade given what the slot already has.</summary>
        public string Describe(SpellUpgradeState state)
        {
            var current = state.Total(kind);
            var next = current + amount;
            var first = state.Rank(kind) == 0;
            var rank = $"Rank {state.Rank(kind) + 1}/{maxRank}";
            return kind switch
            {
                SpellUpgradeKind.ExtraCharge => $"Store an extra cast\nCharges {1 + current:0} → {1 + next:0}\n{rank}",
                SpellUpgradeKind.CooldownReduction => $"Recharges faster\nCooldown {Change("-", current, next, true, first)}\n{rank}",
                SpellUpgradeKind.Damage => $"Hits harder\nDamage {Change("+", current, next, true, first)}\n{rank}",
                SpellUpgradeKind.ExtraProjectiles => $"Casts more at once\nProjectiles {Change("+", current, next, false, first)}\n{rank}",
                SpellUpgradeKind.Duration => $"Lasts longer\nDuration {Change("+", current, next, true, first)}\n{rank}",
                SpellUpgradeKind.Area => $"Covers more ground\nArea {Change("+", current, next, true, first)}\n{rank}",
                SpellUpgradeKind.ExtraBounces => $"Jumps to more enemies\nExtra jumps {Change("+", current, next, false, first)}\n{rank}",
                _ => rank
            };
        }

        /// <summary>"-15%" for a first rank, "-15% → -30%" afterwards.</summary>
        private static string Change(string sign, float current, float next, bool percent, bool first)
        {
            var to = sign + Format(next, percent);
            return first ? to : $"{sign}{Format(current, percent)} → {to}";
        }

        private static string Format(float value, bool percent) => percent
            ? (value * 100f).ToString("0", CultureInfo.InvariantCulture) + "%"
            : value.ToString("0", CultureInfo.InvariantCulture);
    }
}
