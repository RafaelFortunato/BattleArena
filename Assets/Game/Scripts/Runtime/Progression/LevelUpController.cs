using System;
using System.Collections;
using System.Collections.Generic;
using BattleArena.Core;
using BattleArena.Player;
using BattleArena.Spells;
using UnityEngine;
using Random = UnityEngine.Random;

namespace BattleArena.Progression
{
    /// <summary>
    /// Card choices that pause combat: level-ups (a new spell at 5/10/15, stat upgrades otherwise), treasure chests
    /// (stat upgrades, like a level-up) and mission rewards (upgrades for equipped spells). Choices arriving together
    /// are queued and shown one after another.
    /// </summary>
    public sealed class LevelUpController : MonoBehaviour
    {
        /// <summary>A card choice waiting its turn.</summary>
        private readonly struct PendingChoice
        {
            /// <summary>Why the choice is owed.</summary>
            public readonly ChoiceKind Kind;
            /// <summary>The level reached, for level-ups.</summary>
            public readonly int Level;
            /// <summary>The mission's name, for mission rewards.</summary>
            public readonly string Source;

            /// <summary>Creates a pending choice.</summary>
            public PendingChoice(ChoiceKind kind, int level, string source)
            {
                Kind = kind;
                Level = level;
                Source = source;
            }
        }

        [SerializeField] private GameManager gameManager;
        [SerializeField] private InputReader input;
        [SerializeField] private LevelUpConfig config;
        [SerializeField] private PlayerStats stats;
        [SerializeField] private SpellCaster spellCaster;
        [Tooltip("Real-time seconds before a card can be picked, to avoid accidental picks while fighting.")]
        [SerializeField, Min(0f)] private float inputDelay = 0.35f;
        [Tooltip("Real-time seconds between picking a card and the screen closing, so the pick can be seen.")]
        [SerializeField, Min(0f)] private float closeDelay = 0.45f;
        [Tooltip("How strongly Luck boosts rarer cards: weight × (1 + luck × rarityTier × this).")]
        [SerializeField, Min(0f)] private float luckRarityFactor = 1.5f;

        private readonly Queue<PendingChoice> _pending = new();
        private readonly List<LevelUpOffer> _offers = new(3);
        private readonly List<StatUpgradeDefinition> _statCandidates = new();
        private readonly List<SpellDefinition> _spellCandidates = new();
        private readonly List<(int slot, SpellUpgradeDefinition upgrade)> _upgradeCandidates = new();
        private float _acceptInputAt;

        /// <summary>True while cards are on screen and waiting for a pick.</summary>
        public bool IsChoosing { get; private set; }
        /// <summary>Card highlighted for keyboard / gamepad confirmation.</summary>
        public int SelectedIndex { get; private set; }

        /// <summary>Raised when a choice is shown.</summary>
        public event Action<LevelUpPresentation> Presented;
        /// <summary>Raised with the index of the newly highlighted card.</summary>
        public event Action<int> SelectionChanged;
        /// <summary>A card was picked; the screen stays up for <c>closeDelay</c> before <see cref="Closed"/>.</summary>
        public event Action<int> Chosen;
        /// <summary>Raised when the card screen closes after a pick.</summary>
        public event Action Closed;

        /// <summary>Listens for level-ups, chests and the card navigation keys.</summary>
        private void OnEnable()
        {
            EventBus<PlayerLevelUpEvent>.Subscribe(OnLevelUp);
            EventBus<TreasureOpenedEvent>.Subscribe(OnTreasureOpened);
            input.ChoiceNavigated += Navigate;
            input.ConfirmRequested += Confirm;
        }

        /// <summary>Stops listening.</summary>
        private void OnDisable()
        {
            EventBus<PlayerLevelUpEvent>.Unsubscribe(OnLevelUp);
            EventBus<TreasureOpenedEvent>.Unsubscribe(OnTreasureOpened);
            input.ChoiceNavigated -= Navigate;
            input.ConfirmRequested -= Confirm;
        }

        /// <summary>Queues a choice of upgrades for the player's equipped spells (e.g. a mission reward).</summary>
        public void QueueSpellUpgradeReward(string source) => Enqueue(new PendingChoice(ChoiceKind.MissionReward, 0, source));

        /// <summary>Highlights a card (keyboard navigation or mouse hover).</summary>
        public void Select(int index)
        {
            if (!IsChoosing)
                return;
            index = Mathf.Clamp(index, 0, _offers.Count - 1);
            if (index == SelectedIndex)
                return;
            SelectedIndex = index;
            SelectionChanged?.Invoke(index);
        }

        /// <summary>Moves the highlight by <paramref name="direction"/> cards.</summary>
        public void Navigate(int direction) => Select(SelectedIndex + direction);

        /// <summary>Picks the highlighted card.</summary>
        public void Confirm() => Choose(SelectedIndex);

        /// <summary>
        /// Applies the card at <paramref name="index"/> and closes the screen after a short delay. Ignored right after
        /// the cards appear.
        /// </summary>
        public void Choose(int index)
        {
            if (!IsChoosing || index < 0 || index >= _offers.Count || Time.unscaledTime < _acceptInputAt)
                return;

            _offers[index].Apply();
            IsChoosing = false;
            Chosen?.Invoke(index);
            StartCoroutine(CloseAfterDelay());
        }

        /// <summary>
        /// Closes the screen after the close delay in real time (the game stays paused until it closes), then shows the
        /// next queued choice.
        /// </summary>
        private IEnumerator CloseAfterDelay()
        {
            if (closeDelay > 0f)
                yield return new WaitForSecondsRealtime(closeDelay);
            Closed?.Invoke();
            PresentNext();
        }

        /// <summary>Queues a level-up choice.</summary>
        private void OnLevelUp(PlayerLevelUpEvent evt) => Enqueue(new PendingChoice(ChoiceKind.LevelUp, evt.Level, null));

        /// <summary>Queues a treasure choice.</summary>
        private void OnTreasureOpened(TreasureOpenedEvent evt) => Enqueue(new PendingChoice(ChoiceKind.Treasure, 0, null));

        /// <summary>Queues a choice and shows it at once when nothing else is on screen.</summary>
        private void Enqueue(in PendingChoice choice)
        {
            _pending.Enqueue(choice);
            if (!IsChoosing)
                PresentNext();
        }

        /// <summary>
        /// Shows the next queued choice that has cards, pausing combat; resumes combat when the queue is empty. Outside
        /// combat the queue waits.
        /// </summary>
        private void PresentNext()
        {
            while (_pending.Count > 0)
            {
                // Outside combat (e.g. during the defeat slow-motion) the choice simply waits.
                if (!gameManager.BeginLevelUp())
                    return;

                var choice = _pending.Dequeue();
                var presentation = choice.Kind switch
                {
                    ChoiceKind.MissionReward => BuildSpellUpgradeReward(choice.Source),
                    ChoiceKind.Treasure => BuildTreasure(),
                    _ => BuildLevelUp(choice.Level)
                };
                if (_offers.Count == 0)
                    continue;

                IsChoosing = true;
                SelectedIndex = _offers.Count / 2;
                _acceptInputAt = Time.unscaledTime + inputDelay;
                Presented?.Invoke(presentation);
                SelectionChanged?.Invoke(SelectedIndex);
                return;
            }

            gameManager.EndLevelUp();
        }

        // ---------------------------------------------------------------- Level-ups

        /// <summary>Spell cards when <paramref name="level"/> opens a slot, stat cards otherwise.</summary>
        private LevelUpPresentation BuildLevelUp(int level)
        {
            _offers.Clear();

            if (spellCaster.TryGetSlotUnlockedAt(level, out var slot))
            {
                AddSpellOffers(slot);
                if (_offers.Count > 0)
                    return new LevelUpPresentation(ChoiceKind.NewSpell, $"LEVEL {level}", "NEW SPELL",
                        $"Choose a spell to cast with {SpellOffer.GetSlotKey(slot)}", _offers);
            }

            AddStatOffers();
            return new LevelUpPresentation(ChoiceKind.LevelUp, $"LEVEL {level}", "LEVEL UP!", "Choose an upgrade", _offers);
        }

        /// <summary>Stat cards for an opened chest.</summary>
        private LevelUpPresentation BuildTreasure()
        {
            _offers.Clear();
            AddStatOffers();
            return new LevelUpPresentation(ChoiceKind.Treasure, "TREASURE CHEST", "TREASURE!", "Choose an upgrade", _offers);
        }

        /// <summary>Offers a random selection of spells the player doesn't own for <paramref name="slot"/>.</summary>
        private void AddSpellOffers(int slot)
        {
            _spellCandidates.Clear();
            foreach (var spell in config.SpellPool)
                if (spell != null && !spellCaster.Owns(spell))
                    _spellCandidates.Add(spell);

            // Partial Fisher-Yates: only shuffle as many as we need.
            var count = Mathf.Min(config.ChoiceCount, _spellCandidates.Count);
            for (var i = 0; i < count; i++)
            {
                var pick = Random.Range(i, _spellCandidates.Count);
                (_spellCandidates[i], _spellCandidates[pick]) = (_spellCandidates[pick], _spellCandidates[i]);
                _offers.Add(new SpellOffer(_spellCandidates[i], spellCaster, slot));
            }
        }

        /// <summary>
        /// Offers stat cards picked by weight (rarer ones boosted by Luck), never two for the same stat and none for
        /// capped stats.
        /// </summary>
        private void AddStatOffers()
        {
            _statCandidates.Clear();
            foreach (var upgrade in config.StatUpgrades)
                if (upgrade != null && upgrade.Weight > 0f && !stats.IsCapped(upgrade.Stat))
                    _statCandidates.Add(upgrade);

            while (_offers.Count < config.ChoiceCount && _statCandidates.Count > 0)
            {
                var upgrade = PickWeighted(_statCandidates, stats.Luck * luckRarityFactor);
                _offers.Add(new StatOffer(upgrade, stats));

                // Never offer two cards for the same stat at once.
                _statCandidates.RemoveAll(candidate => candidate.Stat == upgrade.Stat);
            }
        }

        // ---------------------------------------------------------------- Spell upgrade rewards

        /// <summary>
        /// Offers upgrades for the equipped spells below their max rank; falls back to stat cards when everything is
        /// maxed.
        /// </summary>
        private LevelUpPresentation BuildSpellUpgradeReward(string source)
        {
            _offers.Clear();
            _upgradeCandidates.Clear();

            // Every (equipped spell, supported upgrade below its max rank) pair is a candidate.
            for (var slot = 0; slot < spellCaster.SlotCount; slot++)
            {
                var spell = spellCaster.GetSpell(slot);
                if (spell == null)
                    continue;

                var upgrades = spellCaster.GetUpgrades(slot);
                foreach (var upgrade in config.SpellUpgrades)
                {
                    if (upgrade != null && upgrade.Weight > 0f && spell.Supports(upgrade.Kind) && upgrades.Rank(upgrade.Kind) < upgrade.MaxRank)
                        _upgradeCandidates.Add((slot, upgrade));
                }
            }

            while (_offers.Count < config.ChoiceCount && _upgradeCandidates.Count > 0)
            {
                var index = PickWeightedIndex(_upgradeCandidates);
                var (slot, upgrade) = _upgradeCandidates[index];
                _upgradeCandidates.RemoveAt(index);
                _offers.Add(new SpellUpgradeOffer(upgrade, spellCaster, slot));
            }

            // Everything maxed out: fall back to stat cards so the reward is never wasted.
            if (_offers.Count == 0)
                AddStatOffers();

            return new LevelUpPresentation(ChoiceKind.MissionReward, source.ToUpperInvariant(), "MISSION COMPLETE",
                "Choose a spell upgrade", _offers);
        }

        /// <summary>Index of a spell upgrade candidate picked at random by weight.</summary>
        private static int PickWeightedIndex(List<(int slot, SpellUpgradeDefinition upgrade)> candidates)
        {
            var total = 0f;
            foreach (var candidate in candidates)
                total += candidate.upgrade.Weight;

            var roll = Random.value * total;
            for (var i = 0; i < candidates.Count; i++)
            {
                roll -= candidates[i].upgrade.Weight;
                if (roll <= 0f)
                    return i;
            }
            return candidates.Count - 1;
        }

        /// <summary>A stat card picked at random by its Luck-boosted weight.</summary>
        private static StatUpgradeDefinition PickWeighted(List<StatUpgradeDefinition> candidates, float rarityBoost)
        {
            var total = 0f;
            foreach (var candidate in candidates)
                total += EffectiveWeight(candidate, rarityBoost);

            var roll = Random.value * total;
            foreach (var candidate in candidates)
            {
                roll -= EffectiveWeight(candidate, rarityBoost);
                if (roll <= 0f)
                    return candidate;
            }
            return candidates[^1];
        }

        /// <summary>A card's weight with rarer tiers boosted by <paramref name="rarityBoost"/>.</summary>
        private static float EffectiveWeight(StatUpgradeDefinition upgrade, float rarityBoost) =>
            upgrade.Weight * (1f + rarityBoost * (int)upgrade.Rarity);
    }
}
