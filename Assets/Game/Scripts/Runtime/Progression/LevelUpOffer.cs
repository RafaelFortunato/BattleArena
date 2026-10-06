using System.Globalization;
using BattleArena.Player;
using BattleArena.Spells;
using UnityEngine;
using UnityEngine.UIElements;

namespace BattleArena.Progression
{
    /// <summary>Background motif of a card; tells rarity (or card type) apart at a glance.</summary>
    public enum CardMotif
    {
        Dots,
        Hexagons,
        Diamonds
    }

    /// <summary>
    /// One card on the level-up screen. Concrete offers know how to present and apply themselves, so the
    /// view stays a dumb binder.
    /// </summary>
    public abstract class LevelUpOffer
    {
        public abstract string Title { get; }
        /// <summary>Short upper-case label on the card badge, e.g. "RARE" or "FIREBALL · Q"; null hides the badge.</summary>
        public abstract string Badge { get; }
        public abstract Color Color { get; }
        public abstract CardMotif Motif { get; }
        public abstract VectorImage Icon { get; }
        /// <summary>Optional line(s) under the title.</summary>
        public virtual string Summary => null;
        public abstract StatPreview Stat { get; }
        /// <summary>For a spell card, which shared stats change the spell; null hides the row.</summary>
        public virtual SpellStats? Scaling => null;

        public abstract void Apply();
    }

    public sealed class StatOffer : LevelUpOffer
    {
        private static readonly Color CommonColor = new(0.663f, 0.706f, 0.784f);
        private static readonly Color RareColor = new(0.302f, 0.639f, 1f);
        private static readonly Color EpicColor = new(0.698f, 0.42f, 1f);

        private readonly StatUpgradeDefinition _upgrade;
        private readonly PlayerStats _stats;

        public StatOffer(StatUpgradeDefinition upgrade, PlayerStats stats)
        {
            _upgrade = upgrade;
            _stats = stats;
        }

        public override string Title => _upgrade.DisplayName;
        public override string Badge => _upgrade.Rarity.ToString().ToUpperInvariant();
        public override VectorImage Icon => _upgrade.Icon;
        public override StatPreview Stat => _stats.Preview(_upgrade.Stat, _upgrade.Amount);

        // One line each, so the card keeps room for its stat, bar and footnote.
        public override string Summary => _upgrade.Stat switch
        {
            StatType.Attack => "All damage scales with Attack.",
            StatType.AttackSpeed => "Basic attacks fire more often.",
            StatType.CooldownReduction => "Every spell recharges faster.",
            StatType.Armor => $"+{ArmorGain().ToString("0", CultureInfo.InvariantCulture)} Armor · 100 Armor halves damage",
            _ => null
        };

        // Only what still fits under the armor cap.
        private float ArmorGain() =>
            Mathf.Min(_upgrade.Amount, PlayerStats.GetCap(StatType.Armor) - _stats.Armor);

        public override Color Color => _upgrade.Rarity switch
        {
            UpgradeRarity.Rare => RareColor,
            UpgradeRarity.Epic => EpicColor,
            _ => CommonColor
        };

        public override CardMotif Motif => _upgrade.Rarity switch
        {
            UpgradeRarity.Rare => CardMotif.Hexagons,
            UpgradeRarity.Epic => CardMotif.Diamonds,
            _ => CardMotif.Dots
        };

        public override void Apply() => _stats.Apply(_upgrade.Stat, _upgrade.Amount);
    }

    /// <summary>Mission reward: improves one equipped spell.</summary>
    public sealed class SpellUpgradeOffer : LevelUpOffer
    {
        private readonly SpellUpgradeDefinition _upgrade;
        private readonly SpellCaster _caster;
        private readonly int _slot;

        public SpellUpgradeOffer(SpellUpgradeDefinition upgrade, SpellCaster caster, int slot)
        {
            _upgrade = upgrade;
            _caster = caster;
            _slot = slot;
        }

        private SpellDefinition Spell => _caster.GetSpell(_slot);

        public override string Title => _upgrade.DisplayName;
        public override string Badge => $"{Spell.DisplayName.ToUpperInvariant()} · {SpellOffer.GetSlotKey(_slot)}";
        public override Color Color => Spell.Color;
        public override CardMotif Motif => CardMotif.Diamonds;
        public override VectorImage Icon => Spell.Icon;
        public override string Summary => _upgrade.Summary;
        public override StatPreview Stat => _upgrade.Preview(_caster.GetUpgrades(_slot), Spell);

        public override void Apply() => _caster.ApplyUpgrade(_slot, _upgrade.Kind, _upgrade.Amount);
    }

    public sealed class SpellOffer : LevelUpOffer
    {
        private static readonly string[] SlotKeys = { "Q", "E", "R" };

        private readonly SpellDefinition _spell;
        private readonly SpellCaster _caster;
        private readonly int _slot;

        public SpellOffer(SpellDefinition spell, SpellCaster caster, int slot)
        {
            _spell = spell;
            _caster = caster;
            _slot = slot;
        }

        public static string GetSlotKey(int slot) => slot >= 0 && slot < SlotKeys.Length ? SlotKeys[slot] : "?";

        public override string Title => _spell.DisplayName;
        // The screen's subtitle already says which key the new spell goes on, so the card needs no badge.
        public override string Badge => null;
        public override Color Color => _spell.Color;
        public override CardMotif Motif => CardMotif.Hexagons;
        public override VectorImage Icon => _spell.Icon;
        public override string Summary => _spell.Description;
        public override SpellStats? Scaling => _spell.ScalesWith;
        // "375% ATK", then the same in numbers at the current Attack, plus the cooldown.
        public override StatPreview Stat
        {
            get
            {
                var cooldown = _spell.Cooldown.ToString("0.#", CultureInfo.InvariantCulture);
                if (_spell.AttackRatio <= 0f)
                {
                    // A utility spell: its own headline (e.g. "Immunity 2s") instead of a damage share.
                    var (label, value) = _spell.CardHeadline;
                    return StatPreview.Plain(label ?? "Cooldown", value ?? cooldown + "s", $"{cooldown}s cooldown");
                }

                var damage = Mathf.RoundToInt(_spell.AttackRatio * _caster.Attack).ToString(CultureInfo.InvariantCulture);
                var unit = string.IsNullOrEmpty(_spell.DamageUnit) ? string.Empty : " " + _spell.DamageUnit;
                return StatPreview.Plain("Damage", SpellDefinition.FormatAttackShare(_spell.AttackRatio) + " ATK",
                    $"{damage} damage{unit} · {cooldown}s cooldown");
            }
        }

        public override void Apply() => _caster.Equip(_slot, _spell);
    }
}
