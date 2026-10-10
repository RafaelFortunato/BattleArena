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
        [Tooltip("Enemies that must die inside the camp's circle, for a mission that starts at 0:00.")]
        [SerializeField, Min(1)] private int killsRequired = 20;
        [Tooltip("Extra kills required per minute of match time when the mission starts (rounded down).")]
        [SerializeField, Min(0f)] private float killsPerMinute;
        [SerializeField, Min(1f)] private float minDistance = 18f;
        [SerializeField, Min(1f)] private float maxDistance = 26f;

        /// <summary>Creates the state for one attempt.</summary>
        public override MissionInstance CreateInstance(MissionContext context) => new Instance(this, context);

        /// <summary>Run-time state: the camp circle and the kills counted inside it.</summary>
        private sealed class Instance : MissionInstance
        {
            private readonly ClearAreaMission _mission;
            private MissionZone _zone;
            private Vector3 _center;
            private readonly int _required;
            private int _kills;

            /// <summary>Fixes the kill target from the match time when the attempt is created.</summary>
            public Instance(ClearAreaMission mission, MissionContext context) : base(mission, context)
            {
                _mission = mission;
                _required = Mathf.FloorToInt(mission.killsRequired + mission.killsPerMinute * context.ElapsedMinutes);
            }

            /// <summary>Share of the required kills made so far.</summary>
            public override float Progress => (float)_kills / _required;

            /// <summary>Kills inside the circle against the target.</summary>
            public override string ProgressText => CachedText("Slain in the circle  {0} / {1}", _kills, _required);

            /// <summary>The description with the kill target filled in.</summary>
            public override string Description => string.Format(Definition.Description, _required);

            /// <summary>
            /// Points at the camp until the player is inside it; once inside, the circle itself is the guide.
            /// </summary>
            public override bool TryGetTarget(out Vector3 position)
            {
                position = _center;
                return _zone != null && !IsInsideCamp(Context.Player.position);
            }

            /// <summary>
            /// Places the camp circle away from the player and spawns its garrison; fails when no spot is free.
            /// </summary>
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

            /// <summary>Counts a kill inside the circle and succeeds once enough have fallen.</summary>
            private void OnEnemyKilled(EnemyKilledEvent evt)
            {
                if (!IsInsideCamp(evt.Position))
                    return;

                _kills++;
                _zone.SetProgress(Progress);
                if (_kills >= _required)
                    Succeed();
            }

            /// <summary>True when <paramref name="position"/> lies within the camp circle.</summary>
            private bool IsInsideCamp(Vector3 position)
            {
                var offset = position - _center;
                offset.y = 0f;
                return offset.sqrMagnitude <= _mission.campRadius * _mission.campRadius;
            }

            /// <summary>
            /// Removes the circle and the kill listener; the garrison simply stays in the fight as regular enemies.
            /// </summary>
            public override void Cleanup()
            {
                EventBus<EnemyKilledEvent>.Unsubscribe(OnEnemyKilled);
                if (_zone != null)
                    _zone.Despawn();
                _zone = null;
            }
        }
    }
}
