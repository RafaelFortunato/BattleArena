using BattleArena.Combat;
using BattleArena.Core;
using UnityEngine;

namespace BattleArena.Player
{
    /// <summary>Experience, levels and the stat growth that comes with them.</summary>
    public sealed class PlayerProgression : MonoBehaviour
    {
        [SerializeField] private Health health;

        [Header("Experience curve: required = base + growth * (level - 1)")]
        [SerializeField, Min(1)] private int baseExperience = 6;
        [SerializeField, Min(0)] private int experienceGrowth = 5;
        [SerializeField, Min(1)] private int maxLevel = 30;

        [Header("Per-level growth")]
        [SerializeField, Min(0f)] private float damageGrowthPerLevel = 0.1f;
        [SerializeField, Min(0f)] private float maxHealthPerLevel = 8f;

        public int Level { get; private set; } = 1;
        public int Experience { get; private set; }
        public int RequiredExperience => baseExperience + experienceGrowth * (Level - 1);
        public bool IsMaxLevel => Level >= maxLevel;

        /// <summary>Multiplier applied to every source of player damage.</summary>
        public float DamageMultiplier => 1f + damageGrowthPerLevel * (Level - 1);

        private void Start() => RaiseExperienceChanged();

        public void AddExperience(int amount)
        {
            if (amount <= 0 || IsMaxLevel)
                return;

            Experience += amount;
            while (!IsMaxLevel && Experience >= RequiredExperience)
            {
                Experience -= RequiredExperience;
                Level++;
                health.IncreaseMax(maxHealthPerLevel);
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
