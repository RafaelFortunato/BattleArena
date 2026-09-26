namespace BattleArena.Core
{
    /// <summary>Physics layer indices. Must match ProjectSettings/TagManager (configured by the content builder).</summary>
    public static class GameLayers
    {
        public const int Default = 0;
        public const int Ground = 6;
        public const int Player = 7;
        public const int Enemy = 8;
        public const int PlayerProjectile = 9;
        public const int EnemyProjectile = 10;
        public const int Pickup = 11;
        public const int Interactable = 12;
        public const int PlayerSensor = 13;

        public const int EnemyMask = 1 << Enemy;
        public const int PlayerMask = 1 << Player;
    }
}
