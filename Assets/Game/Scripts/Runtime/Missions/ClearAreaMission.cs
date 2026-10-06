using BattleArena.Core;
using BattleArena.Enemies;
using BattleArena.Pooling;
using UnityEngine;
using UnityEngine.AI;

namespace BattleArena.Missions
{
    /// <summary>
    /// A large marked camp appears with a garrison inside; defeat enough enemies inside its circle. Any enemy counts,
    /// so the horde that follows the player in helps fill the count.
    /// </summary>
    [CreateAssetMenu(fileName = "ClearAreaMission", menuName = "BattleArena/Missions/Clear Area")]
    public sealed class ClearAreaMission : MissionDefinition
    {
        [SerializeField] private MissionZone zonePrefab;
        [Tooltip("The garrison waiting in the camp: one enemy per entry (repeat an entry to spawn several).")]
        [SerializeField] private EnemyDefinition[] camp;
        [SerializeField, Min(1f)] private float campRadius = 9f;
        [Tooltip("Enemies that must die inside the camp's circle.")]
        [SerializeField, Min(1)] private int killsRequired = 20;
        [SerializeField, Min(1f)] private float minDistance = 18f;
        [SerializeField, Min(1f)] private float maxDistance = 26f;

        public override MissionInstance CreateInstance(MissionContext context) => new Instance(this, context);

        private sealed class Instance : MissionInstance
        {
            private readonly ClearAreaMission _mission;
            private MissionZone _zone;
            private Vector3 _center;
            private int _kills;

            public Instance(ClearAreaMission mission, MissionContext context) : base(mission, context) => _mission = mission;

            public override float Progress => (float)_kills / _mission.killsRequired;

            public override string ProgressText => CachedText("Slain in the circle  {0} / {1}", _kills, _mission.killsRequired);

            // Lead the player to the camp; once inside, the circle itself is the guide.
            public override bool TryGetTarget(out Vector3 position)
            {
                position = _center;
                return _zone != null && !IsInsideCamp(Context.Player.position);
            }

            protected override void OnBegin()
            {
                if (!Context.Spawner.TryFindSpawnPoint(Context.Player.position, _mission.minDistance, _mission.maxDistance, out _center))
                {
                    Fail();
                    return;
                }

                _zone = PoolManager.Spawn(_mission.zonePrefab, _center, Quaternion.identity);
                _zone.Show(_mission.campRadius);

                foreach (var definition in _mission.camp)
                {
                    var offset = Random.insideUnitCircle * (_mission.campRadius * 0.8f);
                    if (NavMesh.SamplePosition(_center + new Vector3(offset.x, 0f, offset.y), out var hit, 2f, NavMesh.AllAreas))
                        Enemy.Spawn(definition, hit.position, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f), 1f, 1f);
                }

                EventBus<EnemyKilledEvent>.Subscribe(OnEnemyKilled);
            }

            private void OnEnemyKilled(EnemyKilledEvent evt)
            {
                if (!IsInsideCamp(evt.Position))
                    return;

                _kills++;
                _zone.SetProgress(Progress);
                if (_kills >= _mission.killsRequired)
                    Succeed();
            }

            private bool IsInsideCamp(Vector3 position)
            {
                var offset = position - _center;
                offset.y = 0f;
                return offset.sqrMagnitude <= _mission.campRadius * _mission.campRadius;
            }

            public override void Cleanup()
            {
                // The garrison simply stays in the fight as regular enemies.
                EventBus<EnemyKilledEvent>.Unsubscribe(OnEnemyKilled);
                if (_zone != null)
                    _zone.Despawn();
                _zone = null;
            }
        }
    }
}
