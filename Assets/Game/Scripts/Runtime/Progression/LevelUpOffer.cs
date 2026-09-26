using System.Globalization;
using BattleArena.Player;
using BattleArena.Spells;
using UnityEngine;

namespace BattleArena.Progression
{
    /// <summary>One card on the level-up screen. Concrete offers know how to apply themselves.</summary>
    public abstract class LevelUpOffer
    {
        public abstract string Title { get; }
        public abstract string Tag { get; }
        public abstract string Description { get; }
        public abstract Color Color { get; }

        public abstract void Apply();
    }

    public sealed class StatOffer : LevelUpOffer
    {
        private static readonly Color CommonColor = new(0.75f, 0.8f, 0.9f);
        private static readonly Color RareColor = new(0.35f, 0.65f, 1f);
        private static readonly Color EpicColor = new(0.8f, 0.45f, 1f);

        private readonly StatUpgradeDefinition _upgrade;
        private readonly PlayerStats _stats;

        public StatOffer(StatUpgradeDefinition upgrade, PlayerStats stats)
        {
            _upgrade = upgrade;
            _stats = stats;
        }

        public override string Title => _upgrade.DisplayName;
        public override string Tag => _upgrade.Rarity.ToString().ToUpperInvariant();
        public override string Description => _stats.DescribeUpgrade(_upgrade.Stat, _upgrade.Amount);

        public override Color Color => _upgrade.Rarity switch
        {
            UpgradeRarity.Rare => RareColor,
            UpgradeRarity.Epic => EpicColor,
            _ => CommonColor
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
        public override string Tag => $"{Spell.DisplayName.ToUpperInvariant()}  ·  [{SpellOffer.GetSlotKey(_slot)}]";
        public override string Description => _upgrade.Describe(_caster.GetUpgrades(_slot));
        public override Color Color => Spell.Color;

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
        public override string Tag => $"SPELL  ·  [{GetSlotKey(_slot)}]";
        public override string Description =>
            _spell.Description + "\n\nCooldown " + _spell.Cooldown.ToString("0.#", CultureInfo.InvariantCulture) + "s";
        public override Color Color => _spell.Color;

        public override void Apply() => _caster.Equip(_slot, _spell);
    }
}
