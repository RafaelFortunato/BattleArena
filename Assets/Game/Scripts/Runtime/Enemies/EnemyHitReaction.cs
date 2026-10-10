using BattleArena.Combat;
using BattleArena.Pooling;
using UnityEngine;

namespace BattleArena.Enemies
{
    /// <summary>
    /// Makes hits land physically: the model squashes on every hit and springs back, and a hit worth a real share of
    /// the enemy's health shoves it a little away from the player. The shove scales with that share, so chip damage
    /// from the auto-attack barely moves anything while a big fireball sends a grunt skidding.
    /// </summary>
    [RequireComponent(typeof(Enemy))]
    public sealed class EnemyHitReaction : MonoBehaviour, IPoolable
    {
        [SerializeField] private Transform visual;

        [Header("Squash")]
        [SerializeField] private Vector3 squash = new(1.16f, 0.84f, 1.16f);
        [SerializeField, Min(0.01f)] private float squashDuration = 0.14f;

        [Header("Knockback")]
        [Tooltip("Shove distance for a hit worth the enemy's whole health; smaller hits push proportionally less.")]
        [SerializeField, Min(0f)] private float knockbackPerHealth = 1.2f;
        [SerializeField, Min(0f)] private float maxKnockback = 0.35f;
        [Tooltip("Hits worth less than this share of max health don't push at all.")]
        [SerializeField, Range(0f, 1f)] private float minKnockbackShare = 0.08f;
        [SerializeField, Min(0.01f)] private float knockbackDuration = 0.12f;
        [Tooltip("1 for ordinary enemies; lower for heavy ones, 0 for those that should never budge.")]
        [SerializeField, Range(0f, 1f)] private float knockbackScale = 1f;

        private Enemy _enemy;
        private Health _health;
        private Vector3 _visualScale = Vector3.one;
        private float _squashElapsed = float.PositiveInfinity;
        private Vector3 _knockbackVelocity;
        private float _knockbackRemaining;

        /// <summary>Caches components and the model's rest scale, and listens for hits.</summary>
        private void Awake()
        {
            // Health directly: Enemy.Awake may not have run yet to expose it.
            _enemy = GetComponent<Enemy>();
            _health = GetComponent<Health>();
            if (visual != null)
                _visualScale = visual.localScale;
            _health.Damaged += OnDamaged;
        }

        /// <summary>Stops listening for hits.</summary>
        private void OnDestroy() => _health.Damaged -= OnDamaged;

        /// <summary>Clears any squash or shove left from the previous life.</summary>
        public void OnSpawned()
        {
            _squashElapsed = float.PositiveInfinity;
            _knockbackRemaining = 0f;
        }

        /// <summary>Nothing to do.</summary>
        public void OnDespawned() { }

        /// <summary>
        /// Starts the squash and, for a big enough hit, a shove away from the player. The killing blow is left to the
        /// death animation.
        /// </summary>
        private void OnDamaged(Health health, DamageResult damage)
        {
            // The killing blow belongs to the death animation, which owns the model's scale from here.
            if (!health.IsAlive)
                return;

            _squashElapsed = 0f;
            TryKnockback(damage.Amount / health.Max);
        }

        /// <summary>
        /// Starts a shove away from the player sized by the hit's share of max health, unless the enemy can't be pushed
        /// right now.
        /// </summary>
        private void TryKnockback(float share)
        {
            var agent = _enemy.Agent;
            // Flyers steer themselves, and an enemy mid-attack (wind-up, dash) holds its ground.
            if (knockbackScale <= 0f || share < minKnockbackShare || _enemy.IsHeld || !agent.enabled || !agent.isOnNavMesh ||
                _enemy.LastTargetPosition is not { } from)
                return;

            var away = transform.position - from;
            away.y = 0f;
            if (away.sqrMagnitude < 0.0001f)
                return;

            var distance = Mathf.Min(maxKnockback, share * knockbackPerHealth) * knockbackScale;
            _knockbackVelocity = away.normalized * (distance / knockbackDuration);
            _knockbackRemaining = knockbackDuration;
        }

        /// <summary>
        /// Plays out the squash and the shove; the shove moves the agent so it never leaves the NavMesh.
        /// </summary>
        private void Update()
        {
            var deltaTime = Time.deltaTime;
            if (deltaTime <= 0f || !_health.IsAlive)
                return;

            if (_squashElapsed < squashDuration && visual != null)
            {
                _squashElapsed += deltaTime;
                // Full squash on impact, easing back to rest.
                var amount = 1f - Mathf.Clamp01(_squashElapsed / squashDuration);
                visual.localScale = Vector3.Scale(_visualScale, Vector3.LerpUnclamped(Vector3.one, squash, amount * amount));
            }

            if (_knockbackRemaining > 0f)
            {
                var step = Mathf.Min(deltaTime, _knockbackRemaining);
                _knockbackRemaining -= step;
                var agent = _enemy.Agent;
                // Move keeps it on the NavMesh, so a shove never pushes an enemy through a wall.
                if (agent.isOnNavMesh)
                    agent.Move(_knockbackVelocity * step);
            }
        }
    }
}
