using BattleArena.Combat;
using UnityEngine;

namespace BattleArena.Missions
{
    /// <summary>
    /// Survive a number of seconds while barely getting hurt: it fails once the damage taken during the mission adds up
    /// to a share of the player's max health, so a stray graze is forgiven but a real beating is not.
    /// The HUD shows the allowance rather than the time (the mission timer already shows that): the bar drains with
    /// every hit, and the description is a format string whose {0} becomes the allowance in HP.
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

            // The bar is the allowance left: full at the start, draining with every hit.
            public override float Progress => Mathf.Clamp01(1f - _damageTaken / Allowance);

            public override string ProgressText =>
                CachedText("{0} / {1} HP left", Mathf.Max(0, Mathf.CeilToInt(Allowance - _damageTaken)), Mathf.CeilToInt(Allowance));

            public override string Description => string.Format(Definition.Description, Mathf.CeilToInt(Allowance));

            /// <summary>Total damage allowed, against max health as it is now (a Max Health upgrade widens it).</summary>
            private float Allowance => Context.PlayerHealth.Max * _mission.allowedDamageFraction;

            protected override void OnBegin() => Context.PlayerHealth.Damaged += OnPlayerDamaged;

            protected override void OnTick(float now, float deltaTime)
            {
                _elapsed += deltaTime;
                if (_elapsed >= _mission.surviveSeconds)
                    Succeed();
            }

            private void OnPlayerDamaged(Health health, DamageResult damage)
            {
                _damageTaken += damage.Amount;
                if (_damageTaken >= Allowance)
                    Fail();
            }

            public override void Cleanup() => Context.PlayerHealth.Damaged -= OnPlayerDamaged;
        }
    }
}
