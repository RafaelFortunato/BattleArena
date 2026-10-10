namespace BattleArena.Core
{
    /// <summary>Physics layer indices. Must match ProjectSettings/TagManager (configured by the content builder).</summary>
    public static class GameLayers
    {
        /// <summary>Unity's default layer: props and scenery.</summary>
        public const int Default = 0;
        /// <summary>The arena floor.</summary>
        public const int Ground = 6;
        /// <summary>The player character.</summary>
        public const int Player = 7;
        /// <summary>Enemies and the boss.</summary>
        public const int Enemy = 8;
        /// <summary>The player's bolts and spells; they hit enemies and obstacles.</summary>
        public const int PlayerProjectile = 9;
        /// <summary>Enemy shots; they hit the player and obstacles.</summary>
        public const int EnemyProjectile = 10;
        /// <summary>Experience gems and other collectibles.</summary>
        public const int Pickup = 11;
        /// <summary>Things the player can use: healing wells and chests.</summary>
        public const int Interactable = 12;
        /// <summary>The player's trigger volumes for pickups and interactions.</summary>
        public const int PlayerSensor = 13;

        /// <summary>Layer mask with only <see cref="Enemy"/>.</summary>
        public const int EnemyMask = 1 << Enemy;
        /// <summary>Layer mask with only <see cref="Player"/>.</summary>
        public const int PlayerMask = 1 << Player;
    }
}
