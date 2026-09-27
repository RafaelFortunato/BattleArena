using System.Collections.Generic;
using BattleArena.Progression;
using BattleArena.UI.Elements;
using UnityEngine;
using UnityEngine.UIElements;

namespace BattleArena.UI
{
    /// <summary>
    /// Card screen for level-ups, new spells and mission rewards. Cards are instantiated from a UXML template;
    /// the highlighted card follows <see cref="LevelUpController.SelectedIndex"/> (keyboard, gamepad or hover).
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class LevelUpView : MonoBehaviour
    {
        [SerializeField] private LevelUpController controller;
        [SerializeField] private VisualTreeAsset cardTemplate;

        private readonly List<CardView> _cards = new(3);
        private VisualElement _screen;
        private VisualElement _cardRow;
        private Label _chip;
        private Label _title;
        private Label _subtitle;

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            _screen = root.Q("level-up");
            _cardRow = root.Q("card-row");
            _chip = root.Q<Label>("choice-chip-label");
            _title = root.Q<Label>("choice-title");
            _subtitle = root.Q<Label>("choice-subtitle");

            controller.Presented += Show;
            controller.SelectionChanged += OnSelectionChanged;
            controller.Closed += Hide;
        }

        private void OnDisable()
        {
            controller.Presented -= Show;
            controller.SelectionChanged -= OnSelectionChanged;
            controller.Closed -= Hide;
        }

        private void Show(LevelUpPresentation presentation)
        {
            _chip.text = presentation.Chip;
            _title.text = presentation.Title;
            _subtitle.text = presentation.Subtitle;
            _screen.EnableInClassList("mission", presentation.Kind == ChoiceKind.MissionReward);

            var offers = presentation.Offers;
            while (_cards.Count < offers.Count)
                _cards.Add(CreateCard(_cards.Count));

            for (var i = 0; i < _cards.Count; i++)
                _cards[i].Bind(i < offers.Count ? offers[i] : null);

            _screen.RemoveFromClassList("hidden");
        }

        private void Hide() => _screen.AddToClassList("hidden");

        private void OnSelectionChanged(int index)
        {
            for (var i = 0; i < _cards.Count; i++)
                _cards[i].SetSelected(i == index);
        }

        private CardView CreateCard(int index)
        {
            var instance = cardTemplate.Instantiate();
            var slot = instance.Q(className: "card-slot");
            _cardRow.Add(slot);

            var card = new CardView(slot);
            card.Button.clicked += () => controller.Choose(index);
            slot.RegisterCallback<PointerEnterEvent>(_ => controller.Select(index));
            return card;
        }

        /// <summary>View wrapper for one card.</summary>
        private sealed class CardView
        {
            private static readonly Color IdleBorder = new(1f, 1f, 1f, 0.3f);
            private static readonly Color MutedSegment = new(0.42f, 0.45f, 0.565f);
            private static readonly Color EmptySegment = new(1f, 1f, 1f, 0.08f);

            private readonly VisualElement _slot;
            private readonly VisualElement _shadow;
            private readonly CardArtElement _art;
            private readonly ShapeElement _hex;
            private readonly VisualElement _icon;
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
            private Color _highlight = Color.white;

            public CardView(VisualElement slot)
            {
                _slot = slot;
                Button = slot.Q<Button>(className: "card");
                _shadow = slot.Q(className: "card-shadow");
                _art = slot.Q<CardArtElement>(className: "card-art");
                _hex = slot.Q<ShapeElement>(className: "card-hex");
                _icon = slot.Q(className: "card-icon");
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
            }

            public Button Button { get; }

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
                _hex.SetColors(null, accent);
                _icon.style.backgroundImage = new StyleBackground(offer.Icon);
                _shadow.style.backgroundColor = Color.Lerp(accent, Color.black, 0.45f);

                _badge.style.backgroundColor = accent;
                _badgeGem.style.backgroundColor = ink;
                _badgeLabel.style.color = ink;
                _badgeLabel.text = offer.Badge;

                _title.text = offer.Title;
                var summary = offer.Summary;
                _summary.text = summary;
                _summary.EnableInClassList("hidden", string.IsNullOrEmpty(summary));

                BindStat(offer.Stat, accent);
            }

            public void SetSelected(bool selected)
            {
                _slot.EnableInClassList("selected", selected);
                var border = selected ? _highlight : IdleBorder;
                Button.style.borderTopColor = border;
                Button.style.borderRightColor = border;
                Button.style.borderBottomColor = border;
                Button.style.borderLeftColor = border;
            }

            private void BindStat(in StatPreview stat, Color accent)
            {
                _statLabel.text = stat.Label;
                _statValue.text = stat.Value;
                _statValue.style.color = stat.Bar == StatBarStyle.None ? Color.white : _highlight;

                _bar.EnableInClassList("hidden", stat.Bar != StatBarStyle.Continuous);
                _segments.EnableInClassList("hidden", stat.Bar != StatBarStyle.Segmented);
                _footnote.EnableInClassList("hidden", stat.Bar == StatBarStyle.None);

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
                        $"{stat.Current} now · <color=#{ColorUtility.ToHtmlStringRGB(_highlight)}>{stat.Gain} from this card</color>";
                }
            }

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
                    segment.style.marginLeft = i == 0 ? 0f : 6f;
                    segment.style.backgroundColor = i < filledNow ? MutedSegment : i < filledAfter ? accent : EmptySegment;
                }
            }
        }
    }
}
