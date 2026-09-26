using System;
using System.Collections.Generic;
using UnityEngine;

namespace BattleArena.Enemies
{
    /// <summary>Time-based wave phases plus difficulty scaling for the whole match.</summary>
    [CreateAssetMenu(fileName = "SpawnSchedule", menuName = "BattleArena/Spawn Schedule")]
    public sealed class SpawnSchedule : ScriptableObject
    {
        [Serializable]
        public struct WeightedEnemy
        {
            public EnemyDefinition Enemy;
            [Min(0f)] public float Weight;
        }

        [Serializable]
        public sealed class Phase
        {
            [Tooltip("Match time (seconds) at which this phase starts.")]
            [Min(0f)] public float StartTime;
            [Min(0.05f)] public float SpawnInterval = 1f;
            [Min(1)] public int SpawnsPerTick = 1;
            [Min(1)] public int MaxAlive = 30;
            public WeightedEnemy[] Enemies = Array.Empty<WeightedEnemy>();

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
        [SerializeField, Min(0f)] private float healthScalingPerMinute = 0.25f;
        [SerializeField, Min(0f)] private float damageScalingPerMinute = 0.1f;

        public float GetHealthMultiplier(float elapsedSeconds) => 1f + healthScalingPerMinute * elapsedSeconds / 60f;
        public float GetDamageMultiplier(float elapsedSeconds) => 1f + damageScalingPerMinute * elapsedSeconds / 60f;

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

        public void CollectEnemies(ISet<EnemyDefinition> results)
        {
            foreach (var phase in phases)
            foreach (var entry in phase.Enemies)
                if (entry.Enemy != null)
                    results.Add(entry.Enemy);
        }

        internal void SetPhases(Phase[] newPhases) => phases = newPhases;
    }
}
