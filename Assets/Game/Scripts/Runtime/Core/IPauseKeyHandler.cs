namespace BattleArena.Core
{
    /// <summary>
    /// Something on screen (a menu or dialog) that gets the pause key before the game pauses or resumes, so the key
    /// can close a dialog or leave Enter to the focused menu button instead.
    /// </summary>
    public interface IPauseKeyHandler
    {
        /// <param name="viaEnter">The key was Enter, which also presses whichever menu button has focus.</param>
        /// <returns>True when the key was used here and the game should not toggle pause.</returns>
        bool HandlePauseKey(bool viaEnter);
    }
}
