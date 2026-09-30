using System;
using BattleArena.Combat;
using BattleArena.Core;
using BattleArena.Pickups;
using BattleArena.Pooling;
using UnityEngine;
using UnityEngine.AI;

namespace BattleArena.Enemies
{
    /// <summary>What an enemy needs to know on each director tick.</summary>
    public readonly struct EnemyTickContext
    {
        public readonly Vector3 TargetPosition;
        public readonly Health Target;
        public readonly float Now;
        /// <summary>Seconds since the previous tick.</summary>
        public readonly float DeltaTime;

        public EnemyTickContext(Vector3 targetPosition, Health target, float now, float deltaTime)
        {
            TargetPosition = targetPosition;
            Target = target;
            Now = now;
            DeltaTime = deltaTime;
        }
    }

    /// <summary>
    /// Pooled NavMesh enemy core: health, rewards, status effects and speed. Behaviour is composed
    /// from an <see cref="EnemyMovement"/> and one or more <see cref="EnemyAttack"/> components
    /// (strategy pattern), so new enemy types are prefab configurations rather than new classes.
    /// It has no Update: <see cref="EnemyDirector"/> ticks every enemy a few times per second.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent), typeof(Health))]
    public sealed class Enemy : MonoBehaviour, IPoolable, ISlowable
    {
        [SerializeField] private ExperienceGem experienceGemPrefab;
        [SerializeField] private TimedRelease deathEffect;

        private NavMeshAgent _agent;
        private Health _health;
        private Collider _collider;
        private EnemyMovement _movement;
        private EnemyAttack[] _attacks;
        private float _slowMultiplier = 1f;
        private float _slowUntil;
        private float _speedOverride = -1f;

        internal int RegistryIndex = -1;

        public EnemyDefinition Definition { get; private set; }
        public Health Health => _health;
        public NavMeshAgent Agent => _agent;
        public float HealthMultiplier { get; private set; } = 1f;
        public float DamageMultiplier { get; private set; } = 1f;

        /// <summary>True while an attack controls the enemy (wind-up, dash, fuse); movement is suspended.</summary>
        public bool IsHeld { get; private set; }

        /// <summary>Movement speed after slows and overrides. Flyers read this since they bypass the agent.</summary>
        public float CurrentSpeed { get; private set; }

        /// <summary>
        /// Raised when an attack starts, with the seconds until it lands (0 for an instant hit), so visuals can
        /// time the swing.
        /// </summary>
        public event Action<float> AttackStarted;

        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _health = GetComponent<Health>();
            _collider = GetComponent<Collider>();
            _movement = GetComponent<EnemyMovement>();
            _attacks = GetComponents<EnemyAttack>();
            _health.Damaged += OnDamaged;
            _health.Died += OnDied;
            // Spread agents across priorities so crowds resolve avoidance more naturally.
            _agent.avoidancePriority = UnityEngine.Random.Range(30, 70);
        }

        private void OnDestroy()
        {
            _health.Damaged -= OnDamaged;
            _health.Died -= OnDied;
        }

        // Also covers destruction on scene unload, where OnDespawned is never called.
        private void OnDisable() => EnemyRegistry.Unregister(this);

        /// <summary>Spawns a pooled enemy and applies its definition and difficulty scaling.</summary>
        public static Enemy Spawn(EnemyDefinition definition, Vector3 position, Quaternion rotation,
            float healthMultiplier, float damageMultiplier)
        {
            var enemy = PoolManager.Spawn(definition.Prefab, position, rotation);
            enemy.Initialize(definition, healthMultiplier, damageMultiplier);
            return enemy;
        }

        public void Initialize(EnemyDefinition definition, float healthMultiplier, float damageMultiplier)
        {
            Definition = definition;
            HealthMultiplier = healthMultiplier;
            DamageMultiplier = damageMultiplier;
            _health.ResetHealth(definition.MaxHealth * healthMultiplier);
            _agent.stoppingDistance = definition.AttackRange * 0.8f;
            _speedOverride = -1f;
            ClearSlow();
        }

        public void OnSpawned()
        {
            // Flyers disable their agent in the prefab and move themselves.
            if (_agent.enabled && !_agent.isOnNavMesh)
                _agent.Warp(transform.position);
            EnemyRegistry.Register(this);
        }

        public void OnDespawned()
        {
            Release();
            _speedOverride = -1f;
            ClearSlow();
            if (_agent.isOnNavMesh)
                _agent.ResetPath();
        }

        /// <summary>Called by <see cref="EnemyDirector"/> on its tick.</summary>
        internal void Tick(in EnemyTickContext context)
        {
            if (!_health.IsAlive)
                return;

            // Slows expire on the director tick instead of a coroutine per enemy.
            if (_slowMultiplier < 1f && context.Now >= _slowUntil)
                ClearSlow();

            if (!IsHeld && _movement != null)
                _movement.Tick(context);

            for (var i = 0; i < _attacks.Length; i++)
            {
                _attacks[i].Tick(context);
                if (!_health.IsAlive || !isActiveAndEnabled)
                    return; // e.g. a self-destruct removed this enemy
            }
        }

        // ---------------------------------------------------------------- Control API for behaviours

        /// <summary>Tells visuals an attack is starting; it lands after <paramref name="secondsToImpact"/>.</summary>
        public void NotifyAttack(float secondsToImpact) => AttackStarted?.Invoke(secondsToImpact);

        /// <summary>Suspends the movement component so an attack can drive the enemy.</summary>
        public void Hold(bool stopMoving = true)
        {
            IsHeld = true;
            if (_agent.isOnNavMesh)
            {
                _agent.isStopped = stopMoving;
                if (stopMoving)
                    _agent.velocity = Vector3.zero;
            }
        }

        public void Release()
        {
            IsHeld = false;
            if (_agent.isOnNavMesh)
                _agent.isStopped = false;
        }

        /// <summary>Moves toward <paramref name="destination"/>, skipping redundant path requests.</summary>
        public void MoveTo(Vector3 destination)
        {
            if (!_agent.isOnNavMesh)
                return;
            if (_agent.hasPath && (_agent.destination - destination).sqrMagnitude < 0.25f)
                return;
            _agent.SetDestination(destination);
        }

        public void Stop()
        {
            if (_agent.isOnNavMesh && _agent.hasPath)
                _agent.ResetPath();
        }

        public void Face(Vector3 position)
        {
            var direction = position - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(direction);
        }

        /// <summary>Forces a movement speed (e.g. during a dash), ignoring slows until cleared.</summary>
        public void OverrideSpeed(float speed)
        {
            _speedOverride = speed;
            RefreshSpeed();
        }

        public void ClearSpeedOverride()
        {
            _speedOverride = -1f;
            RefreshSpeed();
        }

        /// <summary>Removes the enemy without rewards, e.g. after it blows itself up.</summary>
        public void Vanish()
        {
            if (deathEffect != null)
                PoolManager.Spawn(deathEffect, transform.position + Vector3.up, Quaternion.identity);
            this.Despawn();
        }

        public void ApplySlow(float speedMultiplier, float duration)
        {
            if (!isActiveAndEnabled || Definition == null)
                return;

            // The strongest active slow wins; re-applying refreshes the duration.
            _slowMultiplier = Mathf.Min(_slowMultiplier, speedMultiplier);
            _slowUntil = Mathf.Max(_slowUntil, Time.time + duration);
            RefreshSpeed();
        }

        private void ClearSlow()
        {
            _slowMultiplier = 1f;
            _slowUntil = 0f;
            RefreshSpeed();
        }

        private void RefreshSpeed()
        {
            if (Definition == null)
                return;
            CurrentSpeed = _speedOverride >= 0f ? _speedOverride : Definition.MoveSpeed * _slowMultiplier;
            _agent.speed = CurrentSpeed;
        }

        private void OnDamaged(Health _, DamageResult damage)
        {
            // Anchor floating damage numbers just above the enemy's head.
            var position = transform.position;
            position.y = _collider.bounds.max.y;
            EventBus<EnemyDamagedEvent>.Raise(new EnemyDamagedEvent(position, damage.Amount, damage.IsCritical));
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
