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

        [Header("Boss")]
        [SerializeField] private EnemyDefinition boss;
        [SerializeField, Min(1f)] private float bossSpawnDistance = 12f;

        private Coroutine _spawnLoop;
        private float _matchStartTime;

        public EnemyDefinition BossDefinition => boss;

        private void Start() => Prewarm();

        public void Begin(float matchStartTime)
        {
            _matchStartTime = matchStartTime;
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

        public Enemy SpawnBoss()
        {
            if (!TryFindSpawnPoint(bossSpawnDistance, bossSpawnDistance + 4f, out var position))
                position = player.position + Vector3.forward * bossSpawnDistance;

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
                var healthMultiplier = schedule.GetHealthMultiplier(elapsed);
                var damageMultiplier = schedule.GetDamageMultiplier(elapsed);

                var room = phase.MaxAlive - EnemyRegistry.Count;
                var count = Mathf.Min(room, phase.SpawnsPerTick);
                for (var i = 0; i < count; i++)
                {
                    if (TryFindSpawnPoint(minSpawnDistance, maxSpawnDistance, out var position))
                        Spawn(phase.PickEnemy(), position, healthMultiplier, damageMultiplier);
                }

                yield return WaitCache.Get(phase.SpawnInterval);
            }
        }

        private Enemy Spawn(EnemyDefinition definition, Vector3 position, float healthMultiplier, float damageMultiplier)
        {
            var toPlayer = Vector3.ProjectOnPlane(player.position - position, Vector3.up);
            var rotation = toPlayer.sqrMagnitude > 0.01f ? Quaternion.LookRotation(toPlayer) : Quaternion.identity;
            var enemy = PoolManager.Spawn(definition.Prefab, position, rotation);
            enemy.Initialize(definition, healthMultiplier, damageMultiplier);
            return enemy;
        }

        private bool TryFindSpawnPoint(float minDistance, float maxDistance, out Vector3 position)
        {
            var center = player.position;
            for (var attempt = 0; attempt < MaxPlacementAttempts; attempt++)
            {
                var direction = Random.insideUnitCircle.normalized;
                var distance = Random.Range(minDistance, maxDistance);
                var candidate = center + new Vector3(direction.x, 0f, direction.y) * distance;

                if (NavMesh.SamplePosition(candidate, out var hit, 2f, NavMesh.AllAreas))
                {
                    position = hit.position;
                    return true;
                }
            }

            position = default;
            return false;
        }
    }
}
