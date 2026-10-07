using System.Globalization;
using BattleArena.Progression;
using UnityEngine;
using UnityEngine.UIElements;

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
        [Tooltip("Shown in the card's emblem: what the upgrade does. The spell itself is named in the card's header.")]
        [SerializeField] private VectorImage icon;

        public string DisplayName => displayName;
        public SpellUpgradeKind Kind => kind;
        public float Amount => amount;

        /// <summary>What one pick gives <paramref name="spell"/>: the base amount times the spell's own scale for this kind.</summary>
        public float AmountFor(SpellDefinition spell) => amount * spell.UpgradeScale(kind);
        public int MaxRank => maxRank;
        public float Weight => weight;
        public VectorImage Icon => icon;

        /// <summary>One-line pitch shown under the card title.</summary>
        public string Summary => kind switch
        {
            SpellUpgradeKind.ExtraCharge => "Store an extra cast",
            SpellUpgradeKind.CooldownReduction => "Recharges faster",
            SpellUpgradeKind.Damage => "Deals a bigger share of Attack",
            SpellUpgradeKind.ExtraProjectiles => "Casts more at once",
            SpellUpgradeKind.Duration => "Lasts longer",
            SpellUpgradeKind.Area => "Covers more ground",
            SpellUpgradeKind.ExtraBounces => "Jumps to more enemies",
            _ => string.Empty
        };

        /// <summary>What the card changes for <paramref name="spell"/> with <paramref name="state"/>; the bar tracks upgrade ranks.</summary>
        public StatPreview Preview(SpellUpgradeState state, SpellDefinition spell)
        {
            var amount = AmountFor(spell);
            var current = state.Total(kind);
            var next = current + amount;
            var rank = state.Rank(kind);
            var currentFill = (float)rank / maxRank;
            var afterFill = (float)(rank + 1) / maxRank;

            return kind switch
            {
                SpellUpgradeKind.ExtraCharge => Build("Charges", Count(1f + current, false), Count(1f + next, false), Count(amount, true)),
                SpellUpgradeKind.CooldownReduction => Build("Cooldown", Percent(-current), Percent(-next), Percent(-amount)),
                // Shown as the spell's share of Attack, so the card says exactly how hard the spell will hit.
                // Shares are rounded first so "375% now" plus the gain adds up to the value shown.
                SpellUpgradeKind.Damage => Build("Damage",
                    SpellDefinition.FormatAttackShare(spell.AttackRatio * (1f + current)),
                    SpellDefinition.FormatAttackShare(spell.AttackRatio * (1f + next)) + " ATK",
                    "+" + SpellDefinition.FormatAttackShare(
                        Mathf.Round(spell.AttackRatio * (1f + next) * 100f) / 100f - Mathf.Round(spell.AttackRatio * (1f + current) * 100f) / 100f)),
                SpellUpgradeKind.ExtraProjectiles => Build("Projectiles", Count(current, true), Count(next, true), Count(amount, true)),
                SpellUpgradeKind.Duration => Build("Duration", Percent(current), Percent(next), Percent(amount)),
                SpellUpgradeKind.Area => Build("Area", Percent(current), Percent(next), Percent(amount)),
                SpellUpgradeKind.ExtraBounces => Build("Extra jumps", Count(current, true), Count(next, true), Count(amount, true)),
                _ => StatPreview.Plain(displayName, string.Empty)
            };

            StatPreview Build(string label, string now, string after, string gain) =>
                new(label, after, now, gain, StatBarStyle.Segmented, currentFill, afterFill, maxRank);
        }

        /// <summary>"+30%", "-15%", or "0%" when nothing has been collected yet.</summary>
        private static string Percent(float fraction)
        {
            var value = Mathf.RoundToInt(fraction * 100f);
            return value == 0 ? "0%" : (value > 0 ? "+" : "-") + Mathf.Abs(value).ToString(CultureInfo.InvariantCulture) + "%";
        }

        private static string Count(float value, bool signed) =>
            (signed ? "+" : string.Empty) + value.ToString("0", CultureInfo.InvariantCulture);
    }
}
