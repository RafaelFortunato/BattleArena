using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleArena.Enemies
{
    /// <summary>
    /// Time-based wave phases. Enemies never scale: each type has fixed base stats, and later phases get harder by
    /// bringing in tougher enemy types and retiring the early ones.
    /// </summary>
    [CreateAssetMenu(fileName = "SpawnSchedule", menuName = "BattleArena/Spawn Schedule")]
    public sealed class SpawnSchedule : ScriptableObject
    {
        /// <summary>One enemy type in a phase and how often it is picked.</summary>
        [Serializable]
        public struct WeightedEnemy
        {
            /// <summary>The enemy type.</summary>
            public EnemyDefinition Enemy;

            /// <summary>Relative chance of being picked.</summary>
            [Min(0f)] public float Weight;
        }

        /// <summary>A stretch of the match with its own spawn rate, cap and enemy mix.</summary>
        [Serializable]
        public sealed class Phase
        {
            /// <summary>Match time (seconds) at which this phase starts.</summary>
            [Tooltip("Match time (seconds) at which this phase starts.")]
            [Min(0f)] public float StartTime;

            /// <summary>Seconds between spawn ticks.</summary>
            [Min(0.05f)] public float SpawnInterval = 1f;

            /// <summary>Enemies spawned on each tick.</summary>
            [Min(1)] public int SpawnsPerTick = 1;

            /// <summary>No spawns while this many enemies are alive.</summary>
            [Min(1)] public int MaxAlive = 30;

            /// <summary>The enemy mix of this phase.</summary>
            public WeightedEnemy[] Enemies = Array.Empty<WeightedEnemy>();

            /// <summary>Picks an enemy type at random, weighted by the mix.</summary>
            public EnemyDefinition PickEnemy()
            {
                var total = 0f;
                foreach (var entry in Enemies)
                    total += entry.Weight;

                var roll = UnityEngine.Random.value * total;
                foreach (var entry in Enemies)
                {
                    roll -= entry.Weight;
                    if (roll <= 0f)
                        return entry.Enemy;
                }
                return Enemies.Length > 0 ? Enemies[^1].Enemy : null;
            }
        }

        [Tooltip("Phases sorted by start time.")]
        [SerializeField] private Phase[] phases = Array.Empty<Phase>();

        /// <summary>The phase active at <paramref name="elapsedSeconds"/> into the match.</summary>
        public Phase GetPhase(float elapsedSeconds)
        {
            Phase current = null;
            foreach (var phase in phases)
            {
                if (phase.StartTime > elapsedSeconds)
                    break;
                current = phase;
            }
            return current ?? (phases.Length > 0 ? phases[0] : null);
        }

        /// <summary>Adds every enemy type the schedule can spawn to <paramref name="results"/>.</summary>
        public void CollectEnemies(ISet<EnemyDefinition> results)
        {
            foreach (var phase in phases)
            foreach (var entry in phase.Enemies)
                if (entry.Enemy != null)
                    results.Add(entry.Enemy);
        }

        /// <summary>Replaces the phases; used by the content builder.</summary>
        internal void SetPhases(Phase[] newPhases) => phases = newPhases;
    }
}
