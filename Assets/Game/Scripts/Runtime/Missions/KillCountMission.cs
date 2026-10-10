using BattleArena.Core;
using UnityEngine;

namespace BattleArena.Missions
{
    /// <summary>Defeat a number of enemies within the time limit; the number grows with match time.</summary>
    [CreateAssetMenu(fileName = "KillCountMission", menuName = "BattleArena/Missions/Kill Count")]
    public sealed class KillCountMission : MissionDefinition
    {
        [Tooltip("Kills required for a mission that starts at 0:00.")]
        [SerializeField, Min(1)] private int killCount = 40;
        [Tooltip("Extra kills required per minute of match time when the mission starts (rounded down).")]
        [SerializeField, Min(0f)] private float killsPerMinute;

        /// <summary>Creates the state for one attempt.</summary>
        public override MissionInstance CreateInstance(MissionContext context) => new Instance(this, context);

        /// <summary>Run-time state: kills made since the mission started.</summary>
        private sealed class Instance : MissionInstance
        {
            private readonly KillCountMission _mission;
            private readonly int _required;
            private int _kills;

            /// <summary>Fixes the kill target from the match time when the attempt is created.</summary>
            public Instance(KillCountMission mission, MissionContext context) : base(mission, context)
            {
                _mission = mission;
                _required = Mathf.FloorToInt(mission.killCount + mission.killsPerMinute * context.ElapsedMinutes);
            }

            /// <summary>Share of the required kills made so far.</summary>
            public override float Progress => (float)_kills / _required;

            /// <summary>Kills made against the target.</summary>
            public override string ProgressText => CachedText("Enemies defeated  {0} / {1}", _kills, _required);

            /// <summary>The description with the kill target filled in.</summary>
            public override string Description => string.Format(Definition.Description, _required);

            /// <summary>Starts counting kills.</summary>
            protected override void OnBegin() => EventBus<EnemyKilledEvent>.Subscribe(OnEnemyKilled);

            /// <summary>Counts a kill and succeeds once the target is reached.</summary>
            private void OnEnemyKilled(EnemyKilledEvent _)
            {
                if (++_kills >= _required)
                    Succeed();
            }

            /// <summary>Stops counting kills.</summary>
            public override void Cleanup() => EventBus<EnemyKilledEvent>.Unsubscribe(OnEnemyKilled);
        }
    }
}
