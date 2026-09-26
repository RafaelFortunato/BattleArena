using System.Collections;
using BattleArena.Combat;
using BattleArena.Core;
using BattleArena.Pooling;
using UnityEngine;

namespace BattleArena.Player
{
    /// <summary>
    /// Fires magic bolts at the nearest enemy on a fixed interval while combat is active.
    /// Gains an extra bolt every few levels.
    /// </summary>
    public sealed class AutoAttack : MonoBehaviour
    {
        [SerializeField] private Projectile projectilePrefab;
        [SerializeField] private Transform muzzle;
        [SerializeField] private PlayerProgression progression;

        [Header("Stats")]
        [SerializeField, Min(0f)] private float baseDamage = 10f;
        [SerializeField, Min(0.05f)] private float fireInterval = 0.6f;
        [SerializeField, Min(1f)] private float range = 14f;
        [SerializeField, Min(1f)] private float projectileSpeed = 18f;
        [SerializeField, Min(0)] private int pierce;
        [SerializeField, Min(1)] private int levelsPerExtraBolt = 4;
        [SerializeField, Range(0f, 45f)] private float spreadAngle = 12f;
        [SerializeField, Min(1)] private int maxBolts = 5;

        private Coroutine _fireLoop;

        private void OnEnable() => EventBus<GameStateChangedEvent>.Subscribe(OnGameStateChanged);

        private void OnDisable()
        {
            EventBus<GameStateChangedEvent>.Unsubscribe(OnGameStateChanged);
            StopFiring();
        }

        private void OnGameStateChanged(GameStateChangedEvent evt)
        {
            // Pausing uses timeScale, which already freezes the loop, so only start/stop on real combat changes.
            if (evt.Current.IsCombat())
            {
                _fireLoop ??= StartCoroutine(FireLoop());
            }
            else if (evt.Current != GameState.Paused)
            {
                StopFiring();
            }
        }

        private void StopFiring()
        {
            if (_fireLoop == null)
                return;
            StopCoroutine(_fireLoop);
            _fireLoop = null;
        }

        private IEnumerator FireLoop()
        {
            var wait = WaitCache.Get(fireInterval);
            while (true)
            {
                if (CombatQueries.TryFindNearest(muzzle.position, range, GameLayers.EnemyMask, out var target))
                    Fire(target.position);
                yield return wait;
            }
        }

        private void Fire(Vector3 targetPosition)
        {
            var origin = muzzle.position;
            var aim = CombatQueries.Planar(targetPosition - origin);
            var bolts = Mathf.Min(maxBolts, 1 + (progression.Level - 1) / levelsPerExtraBolt);
            var damage = baseDamage * progression.DamageMultiplier;
            var firstAngle = -spreadAngle * (bolts - 1) * 0.5f;

            for (var i = 0; i < bolts; i++)
            {
                var direction = Quaternion.AngleAxis(firstAngle + spreadAngle * i, Vector3.up) * aim;
                var projectile = PoolManager.Spawn(projectilePrefab, origin, Quaternion.LookRotation(direction));
                projectile.Launch(direction, projectileSpeed, damage, pierce);
            }
        }
    }
}
