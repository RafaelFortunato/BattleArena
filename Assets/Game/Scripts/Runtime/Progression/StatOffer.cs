using System.Globalization;
using BattleArena.Player;
using UnityEngine;
using UnityEngine.UIElements;

namespace BattleArena.Progression
{
    /// <summary>A card that permanently raises one character stat; its colour and motif show the rarity.</summary>
    public sealed class StatOffer : LevelUpOffer
    {
        private static readonly Color CommonColor = new(0.663f, 0.706f, 0.784f);
        private static readonly Color RareColor = new(0.302f, 0.639f, 1f);
        private static readonly Color EpicColor = new(0.698f, 0.42f, 1f);

        private readonly StatUpgradeDefinition _upgrade;
        private readonly PlayerStats _stats;

        /// <summary>Creates a card for <paramref name="upgrade"/>, previewed against the player's current stats.</summary>
        public StatOffer(StatUpgradeDefinition upgrade, PlayerStats stats)
        {
            _upgrade = upgrade;
            _stats = stats;
        }

        /// <summary>The upgrade's name.</summary>
        public override string Title => _upgrade.DisplayName;

        /// <summary>The rarity, e.g. "RARE".</summary>
        public override string Badge => _upgrade.Rarity.ToString().ToUpperInvariant();

        /// <summary>The upgrade's icon.</summary>
        public override VectorImage Icon => _upgrade.Icon;

        /// <summary>The stat's value before and after the upgrade.</summary>
        public override StatPreview Stat => _stats.Preview(_upgrade.Stat, _upgrade.Amount);

        /// <summary>A one-line explanation for stats that need one, so the card keeps room for its stat, bar and footnote.</summary>
        public override string Summary => _upgrade.Stat switch
        {
            StatType.Attack => "All damage scales with Attack.",
            StatType.AttackSpeed => "Basic attacks fire more often.",
            StatType.CooldownReduction => "Every spell recharges faster.",
            StatType.Armor => $"+{ArmorGain().ToString("0", CultureInfo.InvariantCulture)} Armor · 100 Armor halves damage",
            _ => null
        };

        /// <summary>The rarity's colour.</summary>
        public override Color Color => _upgrade.Rarity switch
        {
            UpgradeRarity.Rare => RareColor,
            UpgradeRarity.Epic => EpicColor,
            _ => CommonColor
        };

        /// <summary>The rarity's background pattern.</summary>
        public override CardMotif Motif => _upgrade.Rarity switch
        {
            UpgradeRarity.Rare => CardMotif.Hexagons,
            UpgradeRarity.Epic => CardMotif.Diamonds,
            _ => CardMotif.Dots
        };

        /// <summary>Applies the upgrade to the player's stats.</summary>
        public override void Apply() => _stats.Apply(_upgrade.Stat, _upgrade.Amount);

        /// <summary>The armor the card would add: only what still fits under the armor cap.</summary>
        private float ArmorGain() =>
            Mathf.Min(_upgrade.Amount, PlayerStats.GetCap(StatType.Armor) - _stats.Armor);
    }
}
