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

        /// <summary>Creates the state for one attempt.</summary>
        public override MissionInstance CreateInstance(MissionContext context) => new Instance(this, context);

        /// <summary>Run-time state: time survived and damage taken.</summary>
        private sealed class Instance : MissionInstance
        {
            private readonly UntouchableMission _mission;
            private float _elapsed;
            private float _damageTaken;

            /// <summary>Creates the state for one attempt.</summary>
            public Instance(UntouchableMission mission, MissionContext context) : base(mission, context) => _mission = mission;

            /// <summary>The damage allowance left: full at the start, draining with every hit.</summary>
            public override float Progress => Mathf.Clamp01(1f - _damageTaken / Allowance);

            /// <summary>Health the player can still lose against the allowance.</summary>
            public override string ProgressText =>
                CachedText("{0} / {1} HP left", Mathf.Max(0, Mathf.CeilToInt(Allowance - _damageTaken)), Mathf.CeilToInt(Allowance));

            /// <summary>The description with the allowance filled in.</summary>
            public override string Description => string.Format(Definition.Description, Mathf.CeilToInt(Allowance));

            /// <summary>Total damage allowed, against max health as it is now (a Max Health upgrade widens it).</summary>
            private float Allowance => Context.PlayerHealth.Max * _mission.allowedDamageFraction;

            /// <summary>Starts tracking damage to the player.</summary>
            protected override void OnBegin() => Context.PlayerHealth.Damaged += OnPlayerDamaged;

            /// <summary>Succeeds once the player has survived long enough.</summary>
            protected override void OnTick(float now, float deltaTime)
            {
                _elapsed += deltaTime;
                if (_elapsed >= _mission.surviveSeconds)
                    Succeed();
            }

            /// <summary>Adds a hit to the damage taken and fails once the allowance is used up.</summary>
            private void OnPlayerDamaged(Health health, DamageResult damage)
            {
                _damageTaken += damage.Amount;
                if (_damageTaken >= Allowance)
                    Fail();
            }

            /// <summary>Stops tracking damage.</summary>
            public override void Cleanup() => Context.PlayerHealth.Damaged -= OnPlayerDamaged;
        }
    }
}
