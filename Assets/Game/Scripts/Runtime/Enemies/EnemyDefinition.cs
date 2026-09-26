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

        public string DisplayName => displayName;
        public Enemy Prefab => prefab;
        public float MaxHealth => maxHealth;
        public float MoveSpeed => moveSpeed;
        public float ContactDamage => contactDamage;
        public float AttackRange => attackRange;
        public float AttackCooldown => attackCooldown;
        public int ExperienceReward => experienceReward;
    }
}
