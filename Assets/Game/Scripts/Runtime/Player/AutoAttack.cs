using System.Collections;
using BattleArena.Combat;
using BattleArena.Core;
using BattleArena.Pooling;
using UnityEngine;

namespace BattleArena.Player
{
    /// <summary>
    /// Fires magic bolts at the nearest enemy on a fixed interval while combat is active.
    /// Damage, rate, bolt count and pierce all come from <see cref="PlayerStats"/>.
    /// </summary>
    public sealed class AutoAttack : MonoBehaviour
    {
        [SerializeField] private Projectile projectilePrefab;
        [SerializeField] private Transform muzzle;
        [SerializeField] private PlayerStats stats;

        [Header("Stats")]
        [SerializeField, Min(0.05f)] private float fireInterval = 0.6f;
        [SerializeField, Min(1f)] private float range = 14f;
        [SerializeField, Min(1f)] private float projectileSpeed = 18f;
        [SerializeField, Min(0)] private int pierce;
        [SerializeField, Range(0f, 45f)] private float spreadAngle = 12f;
        [SerializeField, Min(1)] private int maxBolts = 11;

        private Coroutine _fireLoop;

        /// <summary>Listens for match state changes.</summary>
        private void OnEnable() => EventBus<GameStateChangedEvent>.Subscribe(OnGameStateChanged);

        /// <summary>Stops listening and stops firing.</summary>
        private void OnDisable()
        {
            EventBus<GameStateChangedEvent>.Unsubscribe(OnGameStateChanged);
            StopFiring();
        }

        /// <summary>
        /// Fires during combat and stops when the match ends. Pause and level-up screens freeze time, which already
        /// stops the loop, so they are left alone.
        /// </summary>
        private void OnGameStateChanged(GameStateChangedEvent evt)
        {
            if (evt.Current.IsCombat())
            {
                _fireLoop ??= StartCoroutine(FireLoop());
            }
            else if (!evt.Current.IsSuspended())
            {
                StopFiring();
            }
        }

        /// <summary>Stops the fire loop if it is running.</summary>
        private void StopFiring()
        {
            if (_fireLoop == null)
                return;
            StopCoroutine(_fireLoop);
            _fireLoop = null;
        }

        /// <summary>Fires at the nearest enemy in range on every interval, shortened by attack speed.</summary>
        private IEnumerator FireLoop()
        {
            var interval = 0f;
            WaitForSeconds wait = null;
            while (true)
            {
                if (CombatQueries.TryFindNearest(muzzle.position, range, GameLayers.EnemyMask, out var target))
                    Fire(target.position);

                // Attack speed shortens the interval; only rebuild the yield instruction when it changes.
                var current = fireInterval / stats.AttackSpeedMultiplier;
                if (wait == null || !Mathf.Approximately(current, interval))
                {
                    interval = current;
                    wait = new WaitForSeconds(interval);
                }
                yield return wait;
            }
        }

        /// <summary>
        /// Fires a fan of bolts at <paramref name="targetPosition"/>. Basic attacks always deal 100% of Attack; they
        /// grow through attack speed, projectiles and pierce.
        /// </summary>
        private void Fire(Vector3 targetPosition)
        {
            var origin = muzzle.position;
            var aim = CombatQueries.AimAt(origin, targetPosition);
            var bolts = Mathf.Min(maxBolts, 1 + stats.ExtraProjectiles);
            var totalPierce = pierce + stats.ExtraPierce;
            var damage = stats.CreateDamage(1f);
            var firstAngle = -spreadAngle * (bolts - 1) * 0.5f;

            for (var i = 0; i < bolts; i++)
            {
                var direction = Quaternion.AngleAxis(firstAngle + spreadAngle * i, Vector3.up) * aim;
                var projectile = PoolManager.Spawn(projectilePrefab, origin, Quaternion.LookRotation(direction));
                projectile.Launch(direction, projectileSpeed, damage, totalPierce);
            }
        }
    }
}
