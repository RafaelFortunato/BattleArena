using BattleArena.Progression;
using BattleArena.Spells;
using BattleArena.UI.Elements;
using UnityEngine;
using UnityEngine.UIElements;

namespace BattleArena.UI
{
    /// <summary>
    /// One card on the level-up screen: binds an offer to the card template and plays its enter, pick and
    /// dismiss motions (USS transitions toggled by classes).
    /// </summary>
    internal sealed class LevelUpCardView
    {
        private static readonly Color IdleBorder = new(1f, 1f, 1f, 0.3f);
        private static readonly Color MutedSegment = new(0.42f, 0.45f, 0.565f);
        private static readonly Color EmptySegment = new(1f, 1f, 1f, 0.08f);

        private readonly VisualElement _slot;
        private readonly VisualElement _shadow;
        private readonly CardArtElement _art;
        private readonly ShapeElement _hex;
        private readonly VisualElement _icon;
        private readonly VisualElement _header;
        private readonly VisualElement _headerBadge;
        private readonly VisualElement _headerIcon;
        private readonly Label _headerLabel;
        private readonly VisualElement _badge;
        private readonly VisualElement _badgeGem;
        private readonly Label _badgeLabel;
        private readonly Label _title;
        private readonly Label _summary;
        private readonly Label _statLabel;
        private readonly Label _statValue;
        private readonly VisualElement _bar;
        private readonly VisualElement _barNow;
        private readonly VisualElement _barGain;
        private readonly VisualElement _segments;
        private readonly Label _footnote;
        private readonly VisualElement _scaling;
        private readonly VisualElement[] _scalingItems;
        private Color _highlight = Color.white;
        private Color _idleBorder = IdleBorder;

        /// <summary>Looks up the card's elements in its template instance.</summary>
        public LevelUpCardView(VisualElement slot)
        {
            _slot = slot;
            Button = slot.Q<Button>(className: "card");
            _shadow = slot.Q(className: "card-shadow");
            _art = slot.Q<CardArtElement>(className: "card-art");
            _hex = slot.Q<ShapeElement>(className: "card-hex");
            _icon = slot.Q(className: "card-icon");
            _header = slot.Q(className: "card-header");
            _headerBadge = slot.Q(className: "card-header-badge");
            _headerIcon = slot.Q(className: "card-header-icon");
            _headerLabel = slot.Q<Label>(className: "card-header-label");
            _badge = slot.Q(className: "card-badge");
            _badgeGem = slot.Q(className: "card-badge-gem");
            _badgeLabel = slot.Q<Label>(className: "card-badge-label");
            _title = slot.Q<Label>(className: "card-title");
            _summary = slot.Q<Label>(className: "card-summary");
            _statLabel = slot.Q<Label>(className: "card-stat-label");
            _statValue = slot.Q<Label>(className: "card-stat-value");
            _bar = slot.Q(className: "card-bar");
            _barNow = slot.Q(className: "card-bar-now");
            _barGain = slot.Q(className: "card-bar-gain");
            _segments = slot.Q(className: "card-segments");
            _footnote = slot.Q<Label>(className: "card-footnote");
            _scaling = slot.Q(className: "card-scaling");
            _scalingItems = _scaling.Query(className: "card-scaling-item").ToList().ToArray();
        }

        /// <summary>The card's button, which picks it.</summary>
        public Button Button { get; }

        /// <summary>Starts below and small, then rises into place with a slight overshoot.</summary>
        public void PlayEnter(long delayMs)
        {
            _slot.RemoveFromClassList("picked");
            _slot.RemoveFromClassList("picked-out");
            _slot.RemoveFromClassList("dismissed");
            _slot.AddToClassList("entering");
            _slot.AddToClassList("animating");
            _slot.schedule.Execute(() => _slot.RemoveFromClassList("entering")).StartingIn(delayMs);
            // Back to the quick hover transition once the card has landed.
            _slot.schedule.Execute(() => _slot.RemoveFromClassList("animating")).StartingIn(delayMs + 400);
        }

        /// <summary>Pops forward, then lifts away and fades.</summary>
        public void PlayPicked(long liftDelayMs)
        {
            _slot.AddToClassList("picked");
            _slot.schedule.Execute(() => _slot.AddToClassList("picked-out")).StartingIn(liftDelayMs);
        }

        /// <summary>Drops down and fades out.</summary>
        public void PlayDismissed() => _slot.AddToClassList("dismissed");

        /// <summary>
        /// Shows <paramref name="offer"/> on the card (or hides the card when null): colours, motif, header for spell
        /// upgrades, badge, text, stat and scaling.
        /// </summary>
        public void Bind(LevelUpOffer offer)
        {
            _slot.EnableInClassList("hidden", offer == null);
            if (offer == null)
                return;

            var accent = offer.Color;
            _highlight = Color.Lerp(accent, Color.white, 0.45f);
            var ink = Color.Lerp(accent, Color.black, 0.82f);

            _art.Accent = accent;
            _art.Pattern = offer.Motif switch
            {
                CardMotif.Dots => CardPattern.Dots,
                CardMotif.Diamonds => CardPattern.Diamonds,
                _ => CardPattern.Hexagons
            };
            // Spell upgrade cards: the spell in a header of its colour, framed in it too, so they stand apart
            // from stat cards and the emblem can show what the upgrade does.
            var upgradedSpell = offer.UpgradedSpell;
            _slot.EnableInClassList("spell-upgrade-card", upgradedSpell != null);
            _header.EnableInClassList("hidden", upgradedSpell == null);
            if (upgradedSpell != null)
            {
                _header.style.backgroundColor = accent;
                _headerBadge.style.backgroundColor = ink;
                _headerIcon.style.backgroundImage = new StyleBackground(upgradedSpell.Icon);
                _headerIcon.style.unityBackgroundImageTintColor = accent;
                _headerLabel.text = upgradedSpell.DisplayName;
                _headerLabel.style.color = ink;
            }
            _idleBorder = upgradedSpell != null ? accent : IdleBorder;
            _hex.SetColors(null, upgradedSpell != null ? _highlight : accent);
            _icon.style.backgroundImage = new StyleBackground(offer.Icon);
            _shadow.style.backgroundColor = Color.Lerp(accent, Color.black, 0.45f);

            _badge.EnableInClassList("hidden", string.IsNullOrEmpty(offer.Badge));
            _badge.style.backgroundColor = accent;
            _badgeGem.style.backgroundColor = ink;
            _badgeLabel.style.color = ink;
            _badgeLabel.text = offer.Badge;
            // Spell cards carry more (damage line and stat row), so their text sits on the bottom edge instead.
            _slot.EnableInClassList("spell-card", offer.Scaling != null);

            _title.text = offer.Title;
            var summary = offer.Summary;
            _summary.text = summary;
            _summary.EnableInClassList("hidden", string.IsNullOrEmpty(summary));

            BindStat(offer.Stat, accent);
            BindScaling(offer.Scaling);
        }

        /// <summary>Lights the Area / Duration / Projectiles / Pierce icons this spell uses, in its colour.</summary>
        private void BindScaling(SpellStats? scaling)
        {
            _scaling.EnableInClassList("hidden", scaling == null);
            if (scaling == null)
                return;

            for (var i = 0; i < _scalingItems.Length; i++)
            {
                var item = _scalingItems[i];
                var used = (scaling.Value & SpellScalingOrder.Stats[i]) != 0;
                item.EnableInClassList("used", used);
                var border = used ? _highlight : SpellScalingOrder.UnusedColor;
                item.style.borderTopColor = border;
                item.style.borderRightColor = border;
                item.style.borderBottomColor = border;
                item.style.borderLeftColor = border;
                item[0].style.unityBackgroundImageTintColor = used ? _highlight : SpellScalingOrder.UnusedColor;
            }
        }

        /// <summary>Highlights the card's border when it is the selected one.</summary>
        public void SetSelected(bool selected)
        {
            _slot.EnableInClassList("selected", selected);
            var border = selected ? _highlight : _idleBorder;
            Button.style.borderTopColor = border;
            Button.style.borderRightColor = border;
            Button.style.borderBottomColor = border;
            Button.style.borderLeftColor = border;
        }

        /// <summary>
        /// Shows the stat: cards that change a value lead with the gain and say where you will end up; spell cards have
        /// no "before", so they show their value (e.g. "375% ATK").
        /// </summary>
        private void BindStat(in StatPreview stat, Color accent)
        {
            _statLabel.text = stat.Label;
            _statValue.text = stat.Bar == StatBarStyle.None ? stat.Value : stat.Gain;
            _statValue.style.color = stat.Bar == StatBarStyle.None ? Color.white : _highlight;

            _bar.EnableInClassList("hidden", stat.Bar != StatBarStyle.Continuous);
            _segments.EnableInClassList("hidden", stat.Bar != StatBarStyle.Segmented);
            _footnote.EnableInClassList("hidden", stat.Bar == StatBarStyle.None && string.IsNullOrEmpty(stat.Note));

            switch (stat.Bar)
            {
                case StatBarStyle.Continuous:
                    _barNow.style.width = Length.Percent(stat.CurrentFill * 100f);
                    _barGain.style.left = Length.Percent(stat.CurrentFill * 100f);
                    _barGain.style.width = Length.Percent(Mathf.Max(0.02f, stat.AfterFill - stat.CurrentFill) * 100f);
                    _barGain.style.backgroundColor = accent;
                    break;
                case StatBarStyle.Segmented:
                    BuildSegments(stat, accent);
                    break;
            }

            if (stat.Bar != StatBarStyle.None)
            {
                _footnote.text =
                    $"{stat.Label} after picking: <color=#{ColorUtility.ToHtmlStringRGB(_highlight)}>{stat.Value}</color>";
            }
            else if (!string.IsNullOrEmpty(stat.Note))
            {
                _footnote.text = stat.Note;
            }
        }

        /// <summary>Draws the segmented bar: ranks already taken, the one this card adds, and the rest.</summary>
        private void BuildSegments(in StatPreview stat, Color accent)
        {
            var count = Mathf.Max(1, stat.Segments);
            var filledNow = Mathf.RoundToInt(stat.CurrentFill * count);
            var filledAfter = Mathf.RoundToInt(stat.AfterFill * count);

            while (_segments.childCount < count)
            {
                var segment = new VisualElement { pickingMode = PickingMode.Ignore };
                segment.AddToClassList("card-segment");
                _segments.Add(segment);
            }

            for (var i = 0; i < _segments.childCount; i++)
            {
                var segment = _segments[i];
                segment.EnableInClassList("hidden", i >= count);
                segment.style.marginLeft = i == 0 ? 0f : 5f;
                segment.style.backgroundColor = i < filledNow ? MutedSegment : i < filledAfter ? accent : EmptySegment;
            }
        }
    }
}
