using System.Collections.Generic;
using BattleArena.Combat;
using BattleArena.Enemies;
using BattleArena.Pooling;
using UnityEngine;
using UnityEngine.AI;

namespace BattleArena.Missions
{
    /// <summary>A marked camp full of enemies appears; kill every one of them.</summary>
    [CreateAssetMenu(fileName = "ClearAreaMission", menuName = "BattleArena/Missions/Clear Area")]
    public sealed class ClearAreaMission : MissionDefinition
    {
        [SerializeField] private MissionZone zonePrefab;
        [Tooltip("One enemy is spawned per entry (repeat an entry to spawn several).")]
        [SerializeField] private EnemyDefinition[] camp;
        [SerializeField, Min(1f)] private float campRadius = 4.5f;
        [SerializeField, Min(1f)] private float minDistance = 15f;
        [SerializeField, Min(1f)] private float maxDistance = 21f;

        public override MissionInstance CreateInstance(MissionContext context) => new Instance(this, context);

        private sealed class Instance : MissionInstance
        {
            private readonly ClearAreaMission _mission;
            private readonly List<Enemy> _remaining = new();
            private MissionZone _zone;
            private Vector3 _center;
            private int _total;

            public Instance(ClearAreaMission mission, MissionContext context) : base(mission, context) => _mission = mission;

            public override float Progress => _total > 0 ? 1f - (float)_remaining.Count / _total : 0f;

            public override string ProgressText => CachedText("Camp cleared  {0} / {1}", _total - _remaining.Count, _total);

            public override bool TryGetTarget(out Vector3 position)
            {
                // Guide the player to the closest survivor once they start leaving the camp.
                position = _center;
                var bestSqr = float.MaxValue;
                var player = Context.Player.position;
                foreach (var enemy in _remaining)
                {
                    var sqr = (enemy.transform.position - player).sqrMagnitude;
                    if (sqr < bestSqr)
                    {
                        bestSqr = sqr;
                        position = enemy.transform.position;
                    }
                }
                return _remaining.Count > 0;
            }

            protected override void OnBegin()
            {
                var spawner = Context.Spawner;
                if (!spawner.TryFindSpawnPoint(Context.Player.position, _mission.minDistance, _mission.maxDistance, out _center))
                {
                    Fail();
                    return;
                }

                _zone = PoolManager.Spawn(_mission.zonePrefab, _center, Quaternion.identity);
                _zone.Show(_mission.campRadius);

                foreach (var definition in _mission.camp)
                {
                    var offset = Random.insideUnitCircle * _mission.campRadius;
                    if (!NavMesh.SamplePosition(_center + new Vector3(offset.x, 0f, offset.y), out var hit, 2f, NavMesh.AllAreas))
                        continue;

                    var enemy = Enemy.Spawn(definition, hit.position, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f),
                        spawner.CurrentHealthMultiplier, spawner.CurrentDamageMultiplier);
                    enemy.Health.Died += OnCampEnemyDied;
                    _remaining.Add(enemy);
                }

                _total = _remaining.Count;
                if (_total == 0)
                    Fail();
            }

            protected override void OnTick(float now, float deltaTime)
            {
                // Enemies that leave without dying (e.g. a Bomb Beetle's self-destruct) still count as cleared.
                for (var i = _remaining.Count - 1; i >= 0; i--)
                {
                    var enemy = _remaining[i];
                    if (enemy.isActiveAndEnabled)
                        continue;
                    enemy.Health.Died -= OnCampEnemyDied;
                    _remaining.RemoveAt(i);
                }
                UpdateProgress();
            }

            private void OnCampEnemyDied(Health health)
            {
                health.Died -= OnCampEnemyDied;
                for (var i = 0; i < _remaining.Count; i++)
                {
                    if (_remaining[i].Health != health)
                        continue;
                    _remaining.RemoveAt(i);
                    break;
                }
                UpdateProgress();
            }

            private void UpdateProgress()
            {
                _zone.SetProgress(Progress);
                if (_remaining.Count == 0)
                    Succeed();
            }

            public override void Cleanup()
            {
                // Survivors simply stay in the fight as regular enemies.
                foreach (var enemy in _remaining)
                    enemy.Health.Died -= OnCampEnemyDied;
                _remaining.Clear();

                if (_zone != null)
                    _zone.Despawn();
                _zone = null;
            }
        }
    }
}
