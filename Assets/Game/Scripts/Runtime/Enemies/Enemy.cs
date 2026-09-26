using System.Collections;
using BattleArena.Combat;
using BattleArena.Core;
using BattleArena.Pickups;
using BattleArena.Pooling;
using UnityEngine;
using UnityEngine.AI;

namespace BattleArena.Enemies
{
    /// <summary>
    /// Pooled NavMesh enemy. It has no Update: <see cref="EnemyDirector"/> ticks every enemy
    /// on a fixed interval to refresh its path and resolve contact attacks.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent), typeof(Health))]
    public sealed class Enemy : MonoBehaviour, IPoolable, ISlowable
    {
        private const float RepathThresholdSqr = 0.5f * 0.5f;

        [SerializeField] private ExperienceGem experienceGemPrefab;
        [SerializeField] private TimedRelease deathEffect;

        private NavMeshAgent _agent;
        private Health _health;
        private float _contactDamage;
        private float _nextAttackTime;
        private Coroutine _slowRoutine;
        private bool _movementLocked;

        internal int RegistryIndex = -1;

        public EnemyDefinition Definition { get; private set; }
        public Health Health => _health;

        /// <summary>Stops the agent in place, e.g. while the boss winds up an attack.</summary>
        public bool MovementLocked
        {
            get => _movementLocked;
            set
            {
                _movementLocked = value;
                if (_agent.isOnNavMesh)
                    _agent.isStopped = value;
            }
        }

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _health = GetComponent<Health>();
            _health.Died += OnDied;
            // Spread agents across priorities so crowds resolve avoidance more naturally.
            _agent.avoidancePriority = Random.Range(30, 70);
        }

        private void OnDestroy() => _health.Died -= OnDied;

        // Also covers destruction on scene unload, where OnDespawned is never called.
        private void OnDisable() => EnemyRegistry.Unregister(this);

        public void Initialize(EnemyDefinition definition, float healthMultiplier, float damageMultiplier)
        {
            Definition = definition;
            _health.ResetHealth(definition.MaxHealth * healthMultiplier);
            _contactDamage = definition.ContactDamage * damageMultiplier;
            _nextAttackTime = 0f;
            _agent.speed = definition.MoveSpeed;
            _agent.stoppingDistance = definition.AttackRange * 0.8f;
        }

        public void OnSpawned()
        {
            if (!_agent.isOnNavMesh)
                _agent.Warp(transform.position);
            EnemyRegistry.Register(this);
        }

        public void OnDespawned()
        {
            StopSlow();
            MovementLocked = false;
            if (_agent.isOnNavMesh)
                _agent.ResetPath();
        }

        /// <summary>Called by <see cref="EnemyDirector"/> on its tick.</summary>
        internal void Tick(Vector3 targetPosition, Health target, float now)
        {
            if (!_health.IsAlive)
                return;

            var offset = targetPosition - transform.position;
            offset.y = 0f;
            var range = Definition.AttackRange;
            if (offset.sqrMagnitude <= range * range && now >= _nextAttackTime)
            {
                target.TakeDamage(_contactDamage);
                _nextAttackTime = now + Definition.AttackCooldown;
            }

            if (!_movementLocked && _agent.isOnNavMesh && (_agent.destination - targetPosition).sqrMagnitude > RepathThresholdSqr)
                _agent.SetDestination(targetPosition);
        }

        public void ApplySlow(float speedMultiplier, float duration)
        {
            if (!isActiveAndEnabled || Definition == null)
                return;

            StopSlow();
            _slowRoutine = StartCoroutine(SlowRoutine(speedMultiplier, duration));
        }

        private IEnumerator SlowRoutine(float speedMultiplier, float duration)
        {
            _agent.speed = Definition.MoveSpeed * speedMultiplier;
            yield return WaitCache.Get(duration);
            _agent.speed = Definition.MoveSpeed;
            _slowRoutine = null;
        }

        private void StopSlow()
        {
            if (_slowRoutine == null)
                return;

            StopCoroutine(_slowRoutine);
            _slowRoutine = null;
            if (Definition != null)
                _agent.speed = Definition.MoveSpeed;
        }

        private void OnDied(Health _)
        {
            var position = transform.position;
            EventBus<EnemyKilledEvent>.Raise(new EnemyKilledEvent(Definition, position));

            var reward = Definition.ExperienceReward;
            if (reward > 0 && experienceGemPrefab != null)
                PoolManager.Spawn(experienceGemPrefab, position + Vector3.up * 0.5f, Quaternion.identity).Initialize(reward);

            if (deathEffect != null)
                PoolManager.Spawn(deathEffect, position + Vector3.up, Quaternion.identity);

            this.Despawn();
        }
    }
}
