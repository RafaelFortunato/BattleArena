namespace BattleArena.Progression
{
    /// <summary>The comparison bar a card draws under its stat.</summary>
    public enum StatBarStyle
    {
        /// <summary>No bar.</summary>
        None,
        /// <summary>One bar: current value, then the gain on top of it.</summary>
        Continuous,
        /// <summary>One segment per step (e.g. projectiles or upgrade ranks).</summary>
        Segmented
    }
}
