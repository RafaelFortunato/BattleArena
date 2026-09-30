using System.Collections;
using BattleArena.Combat;
using BattleArena.Core;
using BattleArena.Pooling;
using UnityEngine;

namespace BattleArena.Enemies
{
    /// <summary>
    /// Boss attack pattern layered on top of the regular <see cref="Enemy"/> chase behaviour.
    /// Alternates telegraphed ground slams with radial projectile bursts and enrages at low health.
    /// </summary>
    [RequireComponent(typeof(Enemy))]
    public sealed class BossController : MonoBehaviour, IPoolable
    {
        [SerializeField, Min(0f)] private float openingDelay = 2f;
        [SerializeField, Min(0.1f)] private float attackInterval = 3.5f;
        [SerializeField, Range(0f, 1f)] private float enrageHealthFraction = 0.5f;
        [SerializeField, Range(0.1f, 1f)] private float enragedIntervalMultiplier = 0.6f;

        [Header("Slam")]
        [SerializeField] private DelayedAreaStrike slamPrefab;
        [SerializeField, Min(1)] private int slamCount = 3;
        [SerializeField, Min(0.5f)] private float slamRadius = 3.5f;
        [SerializeField, Min(0.1f)] private float slamDelay = 1.1f;
        [SerializeField, Min(0f)] private float slamSpacing = 0.45f;
        [SerializeField, Min(0f)] private float slamDamage = 25f;

        [Header("Radial burst")]
        [SerializeField] private Projectile projectilePrefab;
        [SerializeField] private Transform projectileOrigin;
        [SerializeField, Min(3)] private int projectilesPerRing = 16;
        [SerializeField, Min(1)] private int ringsPerBurst = 2;
        [SerializeField, Min(0f)] private float delayBetweenRings = 0.5f;
        [SerializeField, Min(1f)] private float projectileSpeed = 9f;
        [SerializeField, Min(0f)] private float projectileDamage = 10f;

        private Enemy _enemy;
        private Transform _target;

        private bool IsEnraged => _enemy.Health.Normalized <= enrageHealthFraction;

        private void Awake() => _enemy = GetComponent<Enemy>();

        public void SetTarget(Transform target) => _target = target;

        public void OnSpawned() => StartCoroutine(AttackLoop());

        public void OnDespawned() => StopAllCoroutines();

        private IEnumerator AttackLoop()
        {
            yield return WaitCache.Get(openingDelay);

            var useSlam = true;
            while (_enemy.Health.IsAlive)
            {
                if (_target != null)
                    yield return useSlam ? Slam() : RadialBurst();

                useSlam = !useSlam;
                yield return WaitCache.Get(IsEnraged ? attackInterval * enragedIntervalMultiplier : attackInterval);
            }
        }

        private IEnumerator Slam()
        {
            _enemy.Hold();
            _enemy.NotifyAttack(0.3f);
            var count = IsEnraged ? slamCount + 2 : slamCount;

            // Each strike targets where the player is at that moment, forcing them to keep moving.
            for (var i = 0; i < count; i++)
            {
                var position = _target.position;
                var strike = PoolManager.Spawn(slamPrefab, position, Quaternion.identity);
                strike.Arm(slamRadius, slamDelay, DamageProfile.Flat(slamDamage), GameLayers.PlayerMask);
                yield return WaitCache.Get(slamSpacing);
            }

            _enemy.Release();
        }

        private IEnumerator RadialBurst()
        {
            var rings = IsEnraged ? ringsPerBurst + 1 : ringsPerBurst;
            var angleStep = 360f / projectilesPerRing;

            for (var ring = 0; ring < rings; ring++)
            {
                // Offset alternate rings by half a step so they leave different gaps.
                var angleOffset = ring % 2 == 0 ? 0f : angleStep * 0.5f;
                _enemy.NotifyAttack(0f);
                var origin = projectileOrigin.position;

                for (var i = 0; i < projectilesPerRing; i++)
                {
                    var direction = Quaternion.AngleAxis(angleOffset + angleStep * i, Vector3.up) * Vector3.forward;
                    var projectile = PoolManager.Spawn(projectilePrefab, origin, Quaternion.LookRotation(direction));
                    projectile.Launch(direction, projectileSpeed, DamageProfile.Flat(projectileDamage));
                }

                yield return WaitCache.Get(delayBetweenRings);
            }
        }
    }
}
