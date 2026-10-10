using BattleArena.Combat;
using UnityEngine;

namespace BattleArena.Enemies
{
    /// <summary>What an enemy needs to know on each director tick.</summary>
    public readonly struct EnemyTickContext
    {
        /// <summary>The player's position this tick.</summary>
        public readonly Vector3 TargetPosition;

        /// <summary>The player's health, for attacks to damage.</summary>
        public readonly Health Target;

        /// <summary>Game time of this tick.</summary>
        public readonly float Now;

        /// <summary>Seconds since the previous tick.</summary>
        public readonly float DeltaTime;

        /// <summary>Creates the context for one tick.</summary>
        public EnemyTickContext(Vector3 targetPosition, Health target, float now, float deltaTime)
        {
            TargetPosition = targetPosition;
            Target = target;
            Now = now;
            DeltaTime = deltaTime;
        }
    }
}
