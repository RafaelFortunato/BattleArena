using System;
using BattleArena.Player;
using BattleArena.Progression;
using UnityEngine;
using UnityEngine.UIElements;

namespace BattleArena.UI
{
    /// <summary>Shows the three level-up cards and forwards clicks to <see cref="LevelUpController"/>.</summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class LevelUpView : MonoBehaviour
    {
        [SerializeField] private LevelUpController controller;
        [SerializeField] private PlayerStats stats;

        private VisualElement _screen;
        private Label _title;
        private Label _subtitle;
        private Label _summary;
        private CardView[] _cards;

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            _screen = root.Q("level-up");
            _title = root.Q<Label>("level-up-title");
            _subtitle = root.Q<Label>("level-up-subtitle");
            _summary = root.Q<Label>("level-up-stats");

            _cards = new CardView[3];
            for (var i = 0; i < _cards.Length; i++)
            {
                var index = i;
                _cards[i] = new CardView(root.Q<Button>($"card-{i}"), () => controller.Choose(index));
            }

            controller.Presented += Show;
            controller.Closed += Hide;
        }

        private void OnDisable()
        {
            controller.Presented -= Show;
            controller.Closed -= Hide;
            foreach (var card in _cards)
                card.Dispose();
        }

        private void Show(LevelUpPresentation presentation)
        {
            _title.text = presentation.Title;
            _subtitle.text = presentation.Subtitle;

            _summary.text = stats.BuildSummary();

            var offers = presentation.Offers;
            for (var i = 0; i < _cards.Length; i++)
                _cards[i].Bind(i < offers.Count ? offers[i] : null);

            _screen.RemoveFromClassList("hidden");
        }

        private void Hide() => _screen.AddToClassList("hidden");

        /// <summary>View wrapper for one card button.</summary>
        private sealed class CardView : IDisposable
        {
            private readonly Button _button;
            private readonly Action _onClick;
            private readonly Label _tag;
            private readonly Label _title;
            private readonly Label _description;

            public CardView(Button button, Action onClick)
            {
                _button = button;
                _onClick = onClick;
                _tag = button.Q<Label>(className: "card-tag");
                _title = button.Q<Label>(className: "card-title");
                _description = button.Q<Label>(className: "card-description");
                _button.clicked += _onClick;
            }

            public void Bind(LevelUpOffer offer)
            {
                _button.EnableInClassList("hidden", offer == null);
                if (offer == null)
                    return;

                _tag.text = offer.Tag;
                _tag.style.color = offer.Color;
                _title.text = offer.Title;
                _description.text = offer.Description;

                var border = new StyleColor(offer.Color);
                _button.style.borderTopColor = border;
                _button.style.borderBottomColor = border;
                _button.style.borderLeftColor = border;
                _button.style.borderRightColor = border;
            }

            public void Dispose() => _button.clicked -= _onClick;
        }
    }
}
