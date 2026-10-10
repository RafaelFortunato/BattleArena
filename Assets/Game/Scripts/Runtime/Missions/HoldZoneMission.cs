using BattleArena.Pooling;
using UnityEngine;

namespace BattleArena.Missions
{
    /// <summary>Stand inside a circle for a total number of seconds before time runs out.</summary>
    [CreateAssetMenu(fileName = "HoldZoneMission", menuName = "BattleArena/Missions/Hold Zone")]
    public sealed class HoldZoneMission : MissionDefinition
    {
        [SerializeField] private MissionZone zonePrefab;
        [SerializeField, Min(1f)] private float radius = 4f;
        [SerializeField, Min(1f)] private float requiredSeconds = 12f;
        [SerializeField, Min(1f)] private float minDistance = 9f;
        [SerializeField, Min(1f)] private float maxDistance = 15f;

        /// <summary>Creates the state for one attempt.</summary>
        public override MissionInstance CreateInstance(MissionContext context) => new Instance(this, context);

        /// <summary>Run-time state: the circle and the time spent inside it.</summary>
        private sealed class Instance : MissionInstance
        {
            private readonly HoldZoneMission _mission;
            private MissionZone _zone;
            private Vector3 _center;
            private float _held;

            /// <summary>Creates the state for one attempt.</summary>
            public Instance(HoldZoneMission mission, MissionContext context) : base(mission, context) => _mission = mission;

            /// <summary>Share of the required time spent inside.</summary>
            public override float Progress => _held / _mission.requiredSeconds;

            /// <summary>Seconds held against the requirement.</summary>
            public override string ProgressText =>
                CachedText("Hold the circle  {0} / {1}s", Mathf.FloorToInt(_held), Mathf.RoundToInt(_mission.requiredSeconds));

            /// <summary>Points at the circle.</summary>
            public override bool TryGetTarget(out Vector3 position)
            {
                position = _center;
                return true;
            }

            /// <summary>Places the circle away from the player; fails when no spot is free.</summary>
            protected override void OnBegin()
            {
                if (!Context.Spawner.TryFindSpawnPoint(Context.Player.position, _mission.minDistance, _mission.maxDistance, out _center))
                {
                    Fail();
                    return;
                }

                _zone = PoolManager.Spawn(_mission.zonePrefab, _center, Quaternion.identity);
                _zone.Show(_mission.radius);
            }

            /// <summary>
            /// Adds the tick's time while the player stands inside and succeeds once the total is reached.
            /// </summary>
            protected override void OnTick(float now, float deltaTime)
            {
                var offset = Context.Player.position - _center;
                offset.y = 0f;
                if (offset.sqrMagnitude > _mission.radius * _mission.radius)
                    return;

                _held += deltaTime;
                _zone.SetProgress(Progress);
                if (_held >= _mission.requiredSeconds)
                    Succeed();
            }

            /// <summary>Removes the circle.</summary>
            public override void Cleanup()
            {
                if (_zone != null)
                    _zone.Despawn();
                _zone = null;
            }
        }
    }
}
