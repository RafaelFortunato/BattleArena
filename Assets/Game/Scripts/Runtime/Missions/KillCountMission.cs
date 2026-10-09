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

        public override MissionInstance CreateInstance(MissionContext context) => new Instance(this, context);

        private sealed class Instance : MissionInstance
        {
            private readonly KillCountMission _mission;
            private readonly int _required;
            private int _kills;

            public Instance(KillCountMission mission, MissionContext context) : base(mission, context)
            {
                _mission = mission;
                _required = Mathf.FloorToInt(mission.killCount + mission.killsPerMinute * context.ElapsedMinutes);
            }

            public override float Progress => (float)_kills / _required;

            public override string ProgressText => CachedText("Enemies defeated  {0} / {1}", _kills, _required);

            public override string Description => string.Format(Definition.Description, _required);

            protected override void OnBegin() => EventBus<EnemyKilledEvent>.Subscribe(OnEnemyKilled);

            private void OnEnemyKilled(EnemyKilledEvent _)
            {
                if (++_kills >= _required)
                    Succeed();
            }

            public override void Cleanup() => EventBus<EnemyKilledEvent>.Unsubscribe(OnEnemyKilled);
        }
    }
}
