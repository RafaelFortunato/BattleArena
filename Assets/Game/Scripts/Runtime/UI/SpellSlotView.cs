using BattleArena.Spells;
using BattleArena.UI.Elements;
using UnityEngine;
using UnityEngine.UIElements;

namespace BattleArena.UI
{
    /// <summary>
    /// One Q/E/R hexagon: spell icon, charge badge and a clockwise cooldown sweep. States are USS classes
    /// (<c>locked</c>, <c>cooling</c>, <c>cast</c>); texts are only rewritten when their value changes.
    /// </summary>
    internal sealed class SpellSlotView
    {
        private const long CastPulseMs = 120;

        private readonly VisualElement _root;
        private readonly VisualElement _icon;
        private readonly HexCooldownElement _sweep;
        private readonly Label _cooldownText;
        private readonly Label _chargesText;
        private readonly IVisualElementScheduledItem _endPulse;
        private int _shownSeconds = -1;
        private int _shownCharges = -1;

        public SpellSlotView(VisualElement root, int unlockLevel)
        {
            _root = root;
            _icon = root.Q(className: "spell-icon");
            _sweep = root.Q<HexCooldownElement>(className: "spell-sweep");
            _cooldownText = root.Q<Label>(className: "spell-cooldown-text");
            _chargesText = root.Q<Label>(className: "spell-charges");
            root.Q<Label>(className: "spell-lock-level").text = unlockLevel.ToString();
            _endPulse = root.schedule.Execute(() => _root.RemoveFromClassList("cast"));
            _endPulse.Pause();
        }

        public void Bind(SpellDefinition spell)
        {
            _root.EnableInClassList("locked", spell == null);
            if (spell == null)
                return;

            _icon.style.backgroundImage = new StyleBackground(spell.Icon);
            _icon.style.unityBackgroundImageTintColor = spell.Color;
            _chargesText.style.backgroundColor = spell.Color;
        }

        public void PulseCast()
        {
            _root.AddToClassList("cast");
            _endPulse.ExecuteLater(CastPulseMs);
        }

        /// <summary>
        /// The sweep covers the slot only when no charge is left; with charges in stock the slot stays
        /// usable and just shows the count.
        /// </summary>
        public void SetState(int charges, int maxCharges, float remaining, float total)
        {
            var shownCharges = maxCharges > 1 ? charges : -1;
            if (shownCharges != _shownCharges)
            {
                _shownCharges = shownCharges;
                _chargesText.text = shownCharges >= 0 ? shownCharges.ToString() : string.Empty;
                _chargesText.EnableInClassList("visible", shownCharges >= 0);
            }

            var empty = charges <= 0;
            _sweep.Value = empty && total > 0f ? remaining / total : 0f;
            _root.EnableInClassList("cooling", empty);

            var seconds = empty ? Mathf.CeilToInt(remaining) : 0;
            if (seconds == _shownSeconds)
                return;
            _shownSeconds = seconds;
            _cooldownText.text = seconds > 0 ? seconds.ToString() : string.Empty;
        }
    }
}
