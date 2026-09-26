using BattleArena.Core;
using UnityEngine;

namespace BattleArena.Player
{
    /// <summary>
    /// Experience and levels. Each level gained raises <see cref="PlayerLevelUpEvent"/>;
    /// the rewards themselves are chosen through the level-up cards.
    /// </summary>
    public sealed class PlayerProgression : MonoBehaviour
    {
        [SerializeField] private PlayerStats stats;

        [Header("Experience curve: required = base + growth * (level - 1)")]
        [SerializeField, Min(1)] private int baseExperience = 6;
        [SerializeField, Min(0)] private int experienceGrowth = 5;
        [SerializeField, Min(1)] private int maxLevel = 40;

        public int Level { get; private set; } = 1;
        public int Experience { get; private set; }
        public int RequiredExperience => baseExperience + experienceGrowth * (Level - 1);
        public bool IsMaxLevel => Level >= maxLevel;

        // Fractional experience left over after applying the Experience Gain multiplier.
        private float _experienceRemainder;

        private void Start() => RaiseExperienceChanged();

        public void AddExperience(int amount)
        {
            if (amount <= 0 || IsMaxLevel)
                return;

            var scaled = amount * stats.ExperienceMultiplier + _experienceRemainder;
            var gained = Mathf.FloorToInt(scaled);
            _experienceRemainder = scaled - gained;

            Experience += gained;
            while (!IsMaxLevel && Experience >= RequiredExperience)
            {
                Experience -= RequiredExperience;
                Level++;
                EventBus<PlayerLevelUpEvent>.Raise(new PlayerLevelUpEvent(Level));
            }

            if (IsMaxLevel)
                Experience = 0;

            RaiseExperienceChanged();
        }

        private void RaiseExperienceChanged() =>
            EventBus<ExperienceChangedEvent>.Raise(new ExperienceChangedEvent(Level, Experience, RequiredExperience));
    }
}
