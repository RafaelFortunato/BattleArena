using UnityEngine;

namespace BattleArena.Enemies
{
    /// <summary>Data for one enemy archetype. Prefabs stay generic; stats live here.</summary>
    [CreateAssetMenu(fileName = "Enemy", menuName = "BattleArena/Enemy Definition")]
    public sealed class EnemyDefinition : ScriptableObject
    {
        [SerializeField] private string displayName = "Enemy";
        [SerializeField] private Enemy prefab;

        [Header("Stats")]
        [SerializeField, Min(1f)] private float maxHealth = 20f;
        [SerializeField, Min(0f)] private float moveSpeed = 3f;
        [SerializeField, Min(0f)] private float contactDamage = 5f;
        [SerializeField, Min(0.1f)] private float attackRange = 1.2f;
        [SerializeField, Min(0.1f)] private float attackCooldown = 1f;

        [Header("Rewards")]
        [SerializeField, Min(0)] private int experienceReward = 1;

        /// <summary>Name shown to the player (e.g. in the boss banner).</summary>
        public string DisplayName => displayName;
        /// <summary>The pooled prefab spawned for this archetype.</summary>
        public Enemy Prefab => prefab;
        /// <summary>Health before difficulty scaling.</summary>
        public float MaxHealth => maxHealth;
        /// <summary>Movement speed in units per second.</summary>
        public float MoveSpeed => moveSpeed;
        /// <summary>Damage of a melee hit before difficulty scaling.</summary>
        public float ContactDamage => contactDamage;
        /// <summary>Reach of melee hits; also the agent's stopping distance.</summary>
        public float AttackRange => attackRange;
        /// <summary>Seconds between melee hits.</summary>
        public float AttackCooldown => attackCooldown;
        /// <summary>Experience dropped on death.</summary>
        public int ExperienceReward => experienceReward;
    }
}
