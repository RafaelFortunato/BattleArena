namespace BattleArena.Core
{
    public enum GameState
    {
        MainMenu,
        Playing,
        BossFight,
        Paused,
        LevelUp,
        Victory,
        Defeat
    }

    public static class GameStateExtensions
    {
        /// <summary>True while the player is fighting (regular waves or the boss).</summary>
        public static bool IsCombat(this GameState state) => state is GameState.Playing or GameState.BossFight;

        /// <summary>True while combat is frozen by an overlay (pause menu or level-up cards).</summary>
        public static bool IsSuspended(this GameState state) => state is GameState.Paused or GameState.LevelUp;
    }
}
