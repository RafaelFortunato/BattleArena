using BattleArena.Combat;
using UnityEngine;

namespace BattleArena.Missions
{
    /// <summary>Survive a number of seconds without taking any damage. A single hit fails it.</summary>
    [CreateAssetMenu(fileName = "UntouchableMission", menuName = "BattleArena/Missions/Untouchable")]
    public sealed class UntouchableMission : MissionDefinition
    {
        [SerializeField, Min(1f)] private float surviveSeconds = 20f;

        public override MissionInstance CreateInstance(MissionContext context) => new Instance(this, context);

        private sealed class Instance : MissionInstance
        {
            private readonly UntouchableMission _mission;
            private float _elapsed;

            public Instance(UntouchableMission mission, MissionContext context) : base(mission, context) => _mission = mission;

            public override float Progress => _elapsed / _mission.surviveSeconds;

            public override string ProgressText =>
                CachedText("Unharmed for  {0} / {1}s", Mathf.FloorToInt(_elapsed), Mathf.RoundToInt(_mission.surviveSeconds));

            protected override void OnBegin() => Context.PlayerHealth.Damaged += OnPlayerDamaged;

            protected override void OnTick(float now, float deltaTime)
            {
                _elapsed += deltaTime;
                if (_elapsed >= _mission.surviveSeconds)
                    Succeed();
            }

            private void OnPlayerDamaged(Health _, DamageResult __) => Fail();

            public override void Cleanup() => Context.PlayerHealth.Damaged -= OnPlayerDamaged;
        }
    }
}
