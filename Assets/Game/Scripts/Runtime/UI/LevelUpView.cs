using System.Collections.Generic;
using BattleArena.Progression;
using UnityEngine;
using UnityEngine.UIElements;

namespace BattleArena.UI
{
    /// <summary>
    /// Card screen for level-ups, new spells and mission rewards. Cards are instantiated from a UXML template;
    /// the highlighted card follows <see cref="LevelUpController.SelectedIndex"/> (keyboard, gamepad or hover).
    /// Cards fly in one after another, and on a pick the chosen card pops and lifts away while the rest drop out.
    /// The motion is USS transitions toggled by classes; the panel's scheduler runs on real time, so it plays
    /// while the game is paused.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class LevelUpView : MonoBehaviour
    {
        [SerializeField] private LevelUpController controller;
        [SerializeField] private VisualTreeAsset cardTemplate;

        // The first delay gives the panel one frame to lay the cards out in their start pose, so the transition has
        // something to animate from.
        private const long EnterDelayMs = 30;
        private const long EnterStaggerMs = 70;
        private const long PickLiftDelayMs = 160;

        private readonly List<LevelUpCardView> _cards = new(3);
        private VisualElement _screen;
        private VisualElement _cardRow;
        private Label _chip;
        private Label _title;
        private Label _subtitle;

        /// <summary>Finds the screen's elements and follows the level-up controller.</summary>
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
            controller.Chosen += OnChosen;
            controller.Closed += Hide;
        }

        /// <summary>Stops following the controller.</summary>
        private void OnDisable()
        {
            controller.Presented -= Show;
            controller.SelectionChanged -= OnSelectionChanged;
            controller.Chosen -= OnChosen;
            controller.Closed -= Hide;
        }

        /// <summary>
        /// Fills in the screen and cards for <paramref name="presentation"/> and plays the staggered entry.
        /// </summary>
        private void Show(LevelUpPresentation presentation)
        {
            _chip.text = presentation.Chip;
            _title.text = presentation.Title;
            _subtitle.text = presentation.Subtitle;
            _screen.EnableInClassList("mission", presentation.Kind == ChoiceKind.MissionReward);
            _screen.EnableInClassList("treasure", presentation.Kind == ChoiceKind.Treasure);

            var offers = presentation.Offers;
            while (_cards.Count < offers.Count)
                _cards.Add(CreateCard(_cards.Count));

            for (var i = 0; i < _cards.Count; i++)
            {
                _cards[i].Bind(i < offers.Count ? offers[i] : null);
                _cards[i].PlayEnter(EnterDelayMs + i * EnterStaggerMs);
            }

            _screen.RemoveFromClassList("closing");
            _screen.AddToClassList("entering");
            _screen.schedule.Execute(() => _screen.RemoveFromClassList("entering")).StartingIn(EnterDelayMs);
            _screen.RemoveFromClassList("hidden");
        }

        /// <summary>Lifts the picked card away and drops the others.</summary>
        private void OnChosen(int index)
        {
            _screen.AddToClassList("closing");
            for (var i = 0; i < _cards.Count; i++)
            {
                if (i == index)
                    _cards[i].PlayPicked(PickLiftDelayMs);
                else
                    _cards[i].PlayDismissed();
            }
        }

        /// <summary>Hides the screen.</summary>
        private void Hide() => _screen.AddToClassList("hidden");

        /// <summary>Highlights the selected card.</summary>
        private void OnSelectionChanged(int index)
        {
            for (var i = 0; i < _cards.Count; i++)
                _cards[i].SetSelected(i == index);
        }

        /// <summary>
        /// Instantiates a card from the template, wired to pick or select the card at <paramref name="index"/>.
        /// </summary>
        private LevelUpCardView CreateCard(int index)
        {
            var instance = cardTemplate.Instantiate();
            var slot = instance.Q(className: "card-slot");
            _cardRow.Add(slot);

            var card = new LevelUpCardView(slot);
            card.Button.clicked += () => controller.Choose(index);
            slot.RegisterCallback<PointerEnterEvent>(_ => controller.Select(index));
            return card;
        }
    }
}
