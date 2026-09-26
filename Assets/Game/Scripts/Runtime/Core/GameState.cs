namespace BattleArena.Core
{
    public enum GameState
    {
        MainMenu,
        Playing,
        BossFight,
        Paused,
        Victory,
        Defeat
    }

    public static class GameStateExtensions
    {
        /// <summary>True while the player is fighting (regular waves or the boss).</summary>
        public static bool IsCombat(this GameState state) => state is GameState.Playing or GameState.BossFight;
    }
}
