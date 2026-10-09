using BattleArena.Combat;
using UnityEngine;

namespace BattleArena.Missions
{
    /// <summary>
    /// Survive a number of seconds while barely getting hurt: it fails once the damage taken during the mission adds up
    /// to a share of the player's max health, so a stray graze is forgiven but a real beating is not.
    /// </summary>
    [CreateAssetMenu(fileName = "UntouchableMission", menuName = "BattleArena/Missions/Untouchable")]
    public sealed class UntouchableMission : MissionDefinition
    {
        [SerializeField, Min(1f)] private float surviveSeconds = 20f;
        [Tooltip("Total damage the player may take during the mission, as a share of max health; reaching it fails.")]
        [SerializeField, Range(0.01f, 1f)] private float allowedDamageFraction = 0.1f;

        public override MissionInstance CreateInstance(MissionContext context) => new Instance(this, context);

        private sealed class Instance : MissionInstance
        {
            private readonly UntouchableMission _mission;
            private float _elapsed;
            private float _damageTaken;

            public Instance(UntouchableMission mission, MissionContext context) : base(mission, context) => _mission = mission;

            public override float Progress => _elapsed / _mission.surviveSeconds;

            public override string ProgressText =>
                CachedText("Survive  {0} / {1}s", Mathf.FloorToInt(_elapsed), Mathf.RoundToInt(_mission.surviveSeconds));

            protected override void OnBegin() => Context.PlayerHealth.Damaged += OnPlayerDamaged;

            protected override void OnTick(float now, float deltaTime)
            {
                _elapsed += deltaTime;
                if (_elapsed >= _mission.surviveSeconds)
                    Succeed();
            }

            // Against max health as it is now, so a Max Health upgrade mid-mission widens the allowance.
            private void OnPlayerDamaged(Health health, DamageResult damage)
            {
                _damageTaken += damage.Amount;
                if (_damageTaken >= health.Max * _mission.allowedDamageFraction)
                    Fail();
            }

            public override void Cleanup() => Context.PlayerHealth.Damaged -= OnPlayerDamaged;
        }
    }
}
