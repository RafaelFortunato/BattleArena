using BattleArena.Player;
using BattleArena.Spells;
using UnityEngine;
using UnityEngine.UIElements;

namespace BattleArena.Progression
{
    /// <summary>Mission reward: improves one equipped spell.</summary>
    public sealed class SpellUpgradeOffer : LevelUpOffer
    {
        private readonly SpellUpgradeDefinition _upgrade;
        private readonly SpellCaster _caster;
        private readonly int _slot;

        /// <summary>Creates a card that applies <paramref name="upgrade"/> to the spell in <paramref name="slot"/>.</summary>
        public SpellUpgradeOffer(SpellUpgradeDefinition upgrade, SpellCaster caster, int slot)
        {
            _upgrade = upgrade;
            _caster = caster;
            _slot = slot;
        }

        /// <summary>The upgrade's name.</summary>
        public override string Title => _upgrade.DisplayName;

        /// <summary>No badge: the card's header names the spell.</summary>
        public override string Badge => null;

        /// <summary>The upgraded spell's colour.</summary>
        public override Color Color => Spell.Color;

        /// <summary>Spell upgrades always use the diamond pattern.</summary>
        public override CardMotif Motif => CardMotif.Diamonds;

        /// <summary>The upgrade's icon, or the spell's when the upgrade has none.</summary>
        public override VectorImage Icon => _upgrade.Icon != null ? _upgrade.Icon : Spell.Icon;

        /// <summary>What the upgrade does.</summary>
        public override string Summary => _upgrade.Summary;

        /// <summary>The spell the card improves.</summary>
        public override SpellDefinition UpgradedSpell => Spell;

        /// <summary>The upgrade's rank or value before and after.</summary>
        public override StatPreview Stat => _upgrade.Preview(_caster.GetUpgrades(_slot), Spell);

        /// <summary>Applies the upgrade, scaled for this spell, to its slot.</summary>
        public override void Apply() => _caster.ApplyUpgrade(_slot, _upgrade.Kind, _upgrade.AmountFor(Spell));

        /// <summary>The spell in the card's slot.</summary>
        private SpellDefinition Spell => _caster.GetSpell(_slot);
    }
}
