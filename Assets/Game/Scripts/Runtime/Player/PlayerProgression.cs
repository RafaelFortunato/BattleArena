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

        // Early levels come quickly (the spell unlocks at 5, 10 and 15 land within a run), then the cubic term takes
        // over: about level 20 in a typical ten-minute run, while level 40 needs several runs' worth of experience.
        [Header("Experience curve: required = base + growth*n + squared*n² + cubed*n³, n = level - 1")]
        [SerializeField, Min(1)] private int baseExperience = 6;
        [SerializeField, Min(0)] private float experienceGrowth = 5f;
        [SerializeField, Min(0)] private float experienceGrowthSquared = 0.25f;
        [SerializeField, Min(0)] private float experienceGrowthCubed = 0.03f;
        [SerializeField, Min(1)] private int maxLevel = 40;

        public int Level { get; private set; } = 1;
        public int Experience { get; private set; }
        public int RequiredExperience
        {
            get
            {
                var n = Level - 1;
                return Mathf.RoundToInt(baseExperience + experienceGrowth * n + experienceGrowthSquared * n * n + experienceGrowthCubed * n * n * n);
            }
        }
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
            EventBus<ExperienceChangedEvent>.Raise(new ExperienceChangedEvent(Level, Experience, RequiredExperience, maxLevel));
    }
}
