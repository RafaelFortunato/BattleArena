using BattleArena.Core;
using UnityEngine;

namespace BattleArena.Missions
{
    /// <summary>Defeat a number of enemies within the time limit.</summary>
    [CreateAssetMenu(fileName = "KillCountMission", menuName = "BattleArena/Missions/Kill Count")]
    public sealed class KillCountMission : MissionDefinition
    {
        [SerializeField, Min(1)] private int killCount = 40;

        public override MissionInstance CreateInstance(MissionContext context) => new Instance(this, context);

        private sealed class Instance : MissionInstance
        {
            private readonly KillCountMission _mission;
            private int _kills;

            public Instance(KillCountMission mission, MissionContext context) : base(mission, context) => _mission = mission;

            public override float Progress => (float)_kills / _mission.killCount;

            public override string ProgressText => CachedText("Enemies defeated  {0} / {1}", _kills, _mission.killCount);

            protected override void OnBegin() => EventBus<EnemyKilledEvent>.Subscribe(OnEnemyKilled);

            private void OnEnemyKilled(EnemyKilledEvent _)
            {
                if (++_kills >= _mission.killCount)
                    Succeed();
            }

            public override void Cleanup() => EventBus<EnemyKilledEvent>.Unsubscribe(OnEnemyKilled);
        }
    }
}
