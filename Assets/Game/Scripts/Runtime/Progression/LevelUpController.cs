using System;
using System.Collections.Generic;
using BattleArena.Core;
using BattleArena.Player;
using BattleArena.Spells;
using UnityEngine;
using Random = UnityEngine.Random;

namespace BattleArena.Progression
{
    /// <summary>What the card screen should show.</summary>
    public readonly struct LevelUpPresentation
    {
        public readonly string Title;
        public readonly string Subtitle;
        public readonly IReadOnlyList<LevelUpOffer> Offers;

        public LevelUpPresentation(string title, string subtitle, IReadOnlyList<LevelUpOffer> offers)
        {
            Title = title;
            Subtitle = subtitle;
            Offers = offers;
        }
    }

    /// <summary>
    /// Card choices that pause combat: level-ups (a new spell at 5/10/15, stat upgrades otherwise) and
    /// mission rewards (upgrades for equipped spells). Choices arriving together are queued and shown
    /// one after another.
    /// </summary>
    public sealed class LevelUpController : MonoBehaviour
    {
        private readonly struct PendingChoice
        {
            public readonly bool IsSpellUpgradeReward;
            public readonly int Level;
            public readonly string Source;

            public PendingChoice(bool isSpellUpgradeReward, int level, string source)
            {
                IsSpellUpgradeReward = isSpellUpgradeReward;
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
        [Tooltip("How strongly Luck boosts rarer cards: weight × (1 + luck × rarityTier × this).")]
        [SerializeField, Min(0f)] private float luckRarityFactor = 1.5f;

        private readonly Queue<PendingChoice> _pending = new();
        private readonly List<LevelUpOffer> _offers = new(3);
        private readonly List<StatUpgradeDefinition> _statCandidates = new();
        private readonly List<SpellDefinition> _spellCandidates = new();
        private readonly List<(int slot, SpellUpgradeDefinition upgrade)> _upgradeCandidates = new();
        private float _acceptInputAt;

        public bool IsChoosing { get; private set; }

        public event Action<LevelUpPresentation> Presented;
        public event Action Closed;

        private void OnEnable()
        {
            EventBus<PlayerLevelUpEvent>.Subscribe(OnLevelUp);
            input.ChoiceRequested += Choose;
        }

        private void OnDisable()
        {
            EventBus<PlayerLevelUpEvent>.Unsubscribe(OnLevelUp);
            input.ChoiceRequested -= Choose;
        }

        /// <summary>Queues a choice of upgrades for the player's equipped spells (e.g. a mission reward).</summary>
        public void QueueSpellUpgradeReward(string source) => Enqueue(new PendingChoice(true, 0, source));

        public void Choose(int index)
        {
            if (!IsChoosing || index < 0 || index >= _offers.Count || Time.unscaledTime < _acceptInputAt)
                return;

            _offers[index].Apply();
            IsChoosing = false;
            Closed?.Invoke();
            PresentNext();
        }

        private void OnLevelUp(PlayerLevelUpEvent evt) => Enqueue(new PendingChoice(false, evt.Level, null));

        private void Enqueue(in PendingChoice choice)
        {
            _pending.Enqueue(choice);
            if (!IsChoosing)
                PresentNext();
        }

        private void PresentNext()
        {
            while (_pending.Count > 0)
            {
                // Outside combat (e.g. during the defeat slow-motion) the choice simply waits.
                if (!gameManager.BeginLevelUp())
                    return;

                var choice = _pending.Dequeue();
                var presentation = choice.IsSpellUpgradeReward ? BuildSpellUpgradeReward(choice.Source) : BuildLevelUp(choice.Level);
                if (_offers.Count == 0)
                    continue;

                IsChoosing = true;
                _acceptInputAt = Time.unscaledTime + inputDelay;
                Presented?.Invoke(presentation);
                return;
            }

            gameManager.EndLevelUp();
        }

        // ---------------------------------------------------------------- Level-ups

        private LevelUpPresentation BuildLevelUp(int level)
        {
            _offers.Clear();

            if (spellCaster.TryGetSlotUnlockedAt(level, out var slot))
            {
                AddSpellOffers(slot);
                if (_offers.Count > 0)
                    return new LevelUpPresentation("CHOOSE A SPELL",
                        $"Level {level}  ·  it will be cast with [{SpellOffer.GetSlotKey(slot)}]", _offers);
            }

            AddStatOffers();
            return new LevelUpPresentation("LEVEL UP!", $"Level {level}  ·  choose an upgrade", _offers);
        }

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

        private void AddStatOffers()
        {
            _statCandidates.Clear();
            foreach (var upgrade in config.StatUpgrades)
                if (upgrade != null && upgrade.Weight > 0f)
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

            return new LevelUpPresentation("MISSION COMPLETE", $"{source}  ·  choose a spell upgrade", _offers);
        }

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

        private static float EffectiveWeight(StatUpgradeDefinition upgrade, float rarityBoost) =>
            upgrade.Weight * (1f + rarityBoost * (int)upgrade.Rarity);
    }
}
