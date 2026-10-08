using System.Collections;
using System.Collections.Generic;
using BattleArena.Core;
using BattleArena.Pooling;
using UnityEngine;
using UnityEngine.AI;

namespace BattleArena.Enemies
{
    /// <summary>Spawns enemies on a ring around the player (outside the camera) following a <see cref="SpawnSchedule"/>.</summary>
    public sealed class EnemySpawner : MonoBehaviour
    {
        private const int MaxPlacementAttempts = 8;

        [SerializeField] private SpawnSchedule schedule;
        [SerializeField] private Transform player;
        [SerializeField, Min(1f)] private float minSpawnDistance = 18f;
        [SerializeField, Min(1f)] private float maxSpawnDistance = 24f;
        [SerializeField, Min(0)] private int prewarmPerEnemyType = 25;
        [Tooltip("Spawn points must lie within this distance of the arena centre on both axes.")]
        [SerializeField, Min(1f)] private float arenaHalfExtent = 38f;

        [Header("Boss")]
        [SerializeField] private EnemyDefinition boss;
        [SerializeField, Min(1f)] private float bossSpawnDistance = 12f;
        [Tooltip("Share of the normal spawn rate kept during the boss fight (the last phase's enemies keep coming, slower).")]
        [SerializeField, Range(0f, 1f)] private float bossFightSpawnRate = 0.3f;

        private Coroutine _spawnLoop;
        private float _matchStartTime;
        private float _rateScale = 1f;

        public EnemyDefinition BossDefinition => boss;

        private void Start() => Prewarm();

        public void Begin(float matchStartTime)
        {
            _matchStartTime = matchStartTime;
            _rateScale = 1f;
            StopSpawning();
            _spawnLoop = StartCoroutine(SpawnLoop());
        }

        public void StopSpawning()
        {
            if (_spawnLoop == null)
                return;
            StopCoroutine(_spawnLoop);
            _spawnLoop = null;
        }

        /// <summary>Keeps the regular spawns going under the boss, at <c>bossFightSpawnRate</c> of the usual rate.</summary>
        public void EnterBossFight()
        {
            _rateScale = bossFightSpawnRate;
            if (_rateScale <= 0f)
                StopSpawning();
        }

        public Enemy SpawnBoss()
        {
            // Fall back to the open centre of the arena when the player is cornered.
            if (!TryFindSpawnPoint(player.position, bossSpawnDistance, bossSpawnDistance + 4f, out var position) &&
                !TryFindSpawnPoint(Vector3.zero, 0f, bossSpawnDistance, out position))
                position = Vector3.zero;

            return Spawn(boss, position, 1f, 1f);
        }

        private void Prewarm()
        {
            var definitions = new HashSet<EnemyDefinition>();
            schedule.CollectEnemies(definitions);
            foreach (var definition in definitions)
                PoolManager.Prewarm(definition.Prefab, prewarmPerEnemyType, transform.position);
        }

        private IEnumerator SpawnLoop()
        {
            while (true)
            {
                var elapsed = Time.time - _matchStartTime;
                var phase = schedule.GetPhase(elapsed);

                var room = phase.MaxAlive - EnemyRegistry.Count;
                var count = Mathf.Min(room, phase.SpawnsPerTick);
                for (var i = 0; i < count; i++)
                {
                    if (TryFindSpawnPoint(player.position, minSpawnDistance, maxSpawnDistance, out var position))
                        Spawn(phase.PickEnemy(), position, 1f, 1f);
                }

                // A lower rate means the same spawns per tick, further apart.
                yield return WaitCache.Get(phase.SpawnInterval / _rateScale);
            }
        }

        private Enemy Spawn(EnemyDefinition definition, Vector3 position, float healthMultiplier, float damageMultiplier)
        {
            var toPlayer = Vector3.ProjectOnPlane(player.position - position, Vector3.up);
            var rotation = toPlayer.sqrMagnitude > 0.01f ? Quaternion.LookRotation(toPlayer) : Quaternion.identity;
            return Enemy.Spawn(definition, position, rotation, healthMultiplier, damageMultiplier);
        }

        /// <summary>Random walkable point inside the arena on a ring around <paramref name="center"/>.</summary>
        public bool TryFindSpawnPoint(Vector3 center, float minDistance, float maxDistance, out Vector3 position)
        {
            for (var attempt = 0; attempt < MaxPlacementAttempts; attempt++)
            {
                var direction = Random.insideUnitCircle.normalized;
                var distance = Random.Range(minDistance, maxDistance);
                var candidate = center + new Vector3(direction.x, 0f, direction.y) * distance;

                // Near a wall half the ring lies outside the arena; skip those instead of snapping to them.
                if (!IsInsideArena(candidate))
                    continue;

                if (NavMesh.SamplePosition(candidate, out var hit, 2f, NavMesh.AllAreas) && IsInsideArena(hit.position))
                {
                    position = hit.position;
                    return true;
                }
            }

            position = default;
            return false;
        }

        private bool IsInsideArena(Vector3 point) =>
            Mathf.Abs(point.x) <= arenaHalfExtent && Mathf.Abs(point.z) <= arenaHalfExtent;
    }
}
