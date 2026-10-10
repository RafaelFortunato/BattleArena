using System.Globalization;
using BattleArena.Player;
using BattleArena.Spells;
using UnityEngine;
using UnityEngine.UIElements;

namespace BattleArena.Progression
{
    /// <summary>A card that equips a new spell in the slot that just opened.</summary>
    public sealed class SpellOffer : LevelUpOffer
    {
        private static readonly string[] SlotKeys = { "Q", "E", "R" };
        private static readonly string[] XboxButtons = { "X", "Y", "B" };
        private static readonly string[] PlayStationButtons = { "Square", "Triangle", "Circle" };

        private readonly SpellDefinition _spell;
        private readonly SpellCaster _caster;
        private readonly int _slot;

        /// <summary>Creates a card that puts <paramref name="spell"/> in <paramref name="slot"/>.</summary>
        public SpellOffer(SpellDefinition spell, SpellCaster caster, int slot)
        {
            _spell = spell;
            _caster = caster;
            _slot = slot;
        }

        /// <summary>The key or gamepad button that casts the spell in <paramref name="slot"/>, for the device in use.</summary>
        public static string GetSlotKey(int slot)
        {
            var names = ControlScheme.Current switch
            {
                ControlDevice.Xbox => XboxButtons,
                ControlDevice.PlayStation => PlayStationButtons,
                _ => SlotKeys
            };
            return slot >= 0 && slot < names.Length ? names[slot] : "?";
        }

        /// <summary>The spell's name.</summary>
        public override string Title => _spell.DisplayName;

        /// <summary>No badge: the screen's subtitle already says which key the new spell goes on.</summary>
        public override string Badge => null;

        /// <summary>The spell's colour.</summary>
        public override Color Color => _spell.Color;

        /// <summary>New spells always use the hexagon pattern.</summary>
        public override CardMotif Motif => CardMotif.Hexagons;

        /// <summary>The spell's icon.</summary>
        public override VectorImage Icon => _spell.Icon;

        /// <summary>The spell's description.</summary>
        public override string Summary => _spell.Description;

        /// <summary>The shared stats that change the spell.</summary>
        public override SpellStats? Scaling => _spell.ScalesWith;

        /// <summary>
        /// The damage as a share of Attack ("375% ATK"), then the same in numbers at the current Attack, plus the
        /// cooldown. Utility spells show their own headline instead (e.g. "Immunity 2s").
        /// </summary>
        public override StatPreview Stat
        {
            get
            {
                var cooldown = _spell.Cooldown.ToString("0.#", CultureInfo.InvariantCulture);
                if (_spell.AttackRatio <= 0f)
                {
                    var (label, value) = _spell.CardHeadline;
                    return StatPreview.Plain(label ?? "Cooldown", value ?? cooldown + "s", $"{cooldown}s cooldown");
                }

                var damage = Mathf.RoundToInt(_spell.AttackRatio * _caster.Attack).ToString(CultureInfo.InvariantCulture);
                var unit = string.IsNullOrEmpty(_spell.DamageUnit) ? string.Empty : " " + _spell.DamageUnit;
                return StatPreview.Plain("Damage", SpellDefinition.FormatAttackShare(_spell.AttackRatio) + " ATK",
                    $"{damage} damage{unit} · {cooldown}s cooldown");
            }
        }

        /// <summary>Equips the spell in its slot.</summary>
        public override void Apply() => _caster.Equip(_slot, _spell);
    }
}
