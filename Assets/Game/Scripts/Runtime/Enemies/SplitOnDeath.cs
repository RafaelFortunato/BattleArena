using BattleArena.Combat;
using UnityEngine;
using UnityEngine.AI;

namespace BattleArena.Enemies
{
    /// <summary>Spawns smaller enemies where this one dies (e.g. a slime splitting in two).</summary>
    [RequireComponent(typeof(Enemy))]
    public sealed class SplitOnDeath : MonoBehaviour
    {
        [SerializeField] private EnemyDefinition splitInto;
        [SerializeField, Min(1)] private int count = 2;
        [SerializeField, Min(0f)] private float spread = 1.2f;

        private Enemy _enemy;
        private Health _health;

        /// <summary>Caches components and listens for death.</summary>
        private void Awake()
        {
            _enemy = GetComponent<Enemy>();
            _health = GetComponent<Health>();
            _health.Died += OnDied;
        }

        /// <summary>Stops listening for death.</summary>
        private void OnDestroy() => _health.Died -= OnDied;

        /// <summary>
        /// Spawns the smaller enemies in a ring where this one died; they inherit its difficulty scaling.
        /// </summary>
        private void OnDied(Health _)
        {
            var center = transform.position;
            var angleStep = 360f / count;
            var startAngle = Random.Range(0f, 360f);

            for (var i = 0; i < count; i++)
            {
                var direction = Quaternion.Euler(0f, startAngle + angleStep * i, 0f) * Vector3.forward;
                var position = center + direction * spread;
                if (!NavMesh.SamplePosition(position, out var hit, spread + 1f, NavMesh.AllAreas))
                    continue;

                Enemy.Spawn(splitInto, hit.position, Quaternion.LookRotation(direction), _enemy.HealthMultiplier, _enemy.DamageMultiplier);
            }
        }
    }
}
