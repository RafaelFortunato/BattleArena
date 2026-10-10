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

        /// <summary>Current level, starting at 1.</summary>
        public int Level { get; private set; } = 1;
        /// <summary>Experience gathered towards the next level.</summary>
        public int Experience { get; private set; }
        /// <summary>Experience needed for the next level, from the experience curve.</summary>
        public int RequiredExperience
        {
            get
            {
                var n = Level - 1;
                return Mathf.RoundToInt(baseExperience + experienceGrowth * n + experienceGrowthSquared * n * n + experienceGrowthCubed * n * n * n);
            }
        }
        /// <summary>True at the level cap.</summary>
        public bool IsMaxLevel => Level >= maxLevel;

        // Fractional experience left over after applying the Experience Gain multiplier.
        private float _experienceRemainder;

        /// <summary>Publishes the starting experience for the HUD.</summary>
        private void Start() => RaiseExperienceChanged();

        /// <summary>
        /// Adds experience (scaled by Experience Gain, keeping fractions) and raises a level-up for every level
        /// reached.
        /// </summary>
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

        /// <summary>Publishes the current level and experience.</summary>
        private void RaiseExperienceChanged() =>
            EventBus<ExperienceChangedEvent>.Raise(new ExperienceChangedEvent(Level, Experience, RequiredExperience, maxLevel));
    }
}
