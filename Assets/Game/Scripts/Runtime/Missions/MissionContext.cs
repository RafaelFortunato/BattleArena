using BattleArena.Combat;
using BattleArena.Core;
using BattleArena.Enemies;
using UnityEngine;

namespace BattleArena.Missions
{
    /// <summary>World access a running mission needs.</summary>
    public sealed class MissionContext
    {
        /// <summary>The player's transform, for distances and spawn points.</summary>
        public readonly Transform Player;

        /// <summary>The player's health, for missions about taking damage.</summary>
        public readonly Health PlayerHealth;

        /// <summary>The enemy spawner, for finding free ground and spawning mission enemies.</summary>
        public readonly EnemySpawner Spawner;

        private readonly GameManager _game;

        /// <summary>Creates the context for the current scene.</summary>
        public MissionContext(Transform player, Health playerHealth, EnemySpawner spawner, GameManager game)
        {
            Player = player;
            PlayerHealth = playerHealth;
            Spawner = spawner;
            _game = game;
        }

        /// <summary>Minutes since the match started.</summary>
        public float ElapsedMinutes => _game.ElapsedTime / 60f;
    }
}
