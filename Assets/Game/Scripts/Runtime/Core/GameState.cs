namespace BattleArena.Core
{
    /// <summary>The phases of a session, owned by <see cref="GameManager"/>.</summary>
    public enum GameState
    {
        /// <summary>Title screen; no match running.</summary>
        MainMenu,
        /// <summary>The survival phase: waves of enemies until the boss arrives.</summary>
        Playing,
        /// <summary>The final phase against the boss.</summary>
        BossFight,
        /// <summary>The pause menu is open; time is frozen.</summary>
        Paused,
        /// <summary>The level-up cards are shown; time is frozen.</summary>
        LevelUp,
        /// <summary>The boss was defeated.</summary>
        Victory,
        /// <summary>The player died.</summary>
        Defeat
    }
}
