using System;
using System.Collections.Generic;
using BattleArena.Core;
using BattleArena.Player;
using BattleArena.Spells;
using UnityEngine;
using Random = UnityEngine.Random;

namespace BattleArena.Progression
{
    /// <summary>What the level-up screen should show.</summary>
    public readonly struct LevelUpPresentation
    {
        public readonly int Level;
        public readonly bool IsSpellChoice;
        public readonly int SlotIndex;
        public readonly IReadOnlyList<LevelUpOffer> Offers;

        public LevelUpPresentation(int level, bool isSpellChoice, int slotIndex, IReadOnlyList<LevelUpOffer> offers)
        {
            Level = level;
            IsSpellChoice = isSpellChoice;
            SlotIndex = slotIndex;
            Offers = offers;
        }
    }

    /// <summary>
    /// Turns level-ups into card choices: a spell for the slot that opens at that level (5/10/15),
    /// otherwise stat upgrades. Several level-ups at once are queued and presented one after another.
    /// </summary>
    public sealed class LevelUpController : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private InputReader input;
        [SerializeField] private LevelUpConfig config;
        [SerializeField] private PlayerStats stats;
        [SerializeField] private SpellCaster spellCaster;
        [Tooltip("Real-time seconds before a card can be picked, to avoid accidental picks while fighting.")]
        [SerializeField, Min(0f)] private float inputDelay = 0.35f;
        [Tooltip("How strongly Luck boosts rarer cards: weight × (1 + luck × rarityTier × this).")]
        [SerializeField, Min(0f)] private float luckRarityFactor = 1.5f;

        private readonly Queue<int> _pendingLevels = new();
        private readonly List<LevelUpOffer> _offers = new(3);
        private readonly List<StatUpgradeDefinition> _statCandidates = new();
        private readonly List<SpellDefinition> _spellCandidates = new();
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

        public void Choose(int index)
        {
            if (!IsChoosing || index < 0 || index >= _offers.Count || Time.unscaledTime < _acceptInputAt)
                return;

            _offers[index].Apply();
            IsChoosing = false;
            Closed?.Invoke();
            PresentNext();
        }

        private void OnLevelUp(PlayerLevelUpEvent evt)
        {
            _pendingLevels.Enqueue(evt.Level);
            if (!IsChoosing)
                PresentNext();
        }

        private void PresentNext()
        {
            while (_pendingLevels.Count > 0)
            {
                // Outside combat (e.g. during the defeat slow-motion) the choice simply waits.
                if (!gameManager.BeginLevelUp())
                    return;

                var level = _pendingLevels.Dequeue();
                var isSpellChoice = BuildOffers(level, out var slot);
                if (_offers.Count == 0)
                    continue;

                IsChoosing = true;
                _acceptInputAt = Time.unscaledTime + inputDelay;
                Presented?.Invoke(new LevelUpPresentation(level, isSpellChoice, slot, _offers));
                return;
            }

            gameManager.EndLevelUp();
        }

        private bool BuildOffers(int level, out int slot)
        {
            _offers.Clear();

            if (spellCaster.TryGetSlotUnlockedAt(level, out slot))
            {
                AddSpellOffers(slot);
                if (_offers.Count > 0)
                    return true;
            }

            AddStatOffers();
            return false;
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
