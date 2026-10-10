namespace BattleArena.Player
{
    /// <summary>Which family of on-screen button prompts to show.</summary>
    public enum ControlDevice
    {
        /// <summary>Keyboard and mouse: key names.</summary>
        KeyboardMouse,
        /// <summary>Any gamepad that is not a PlayStation controller: A/B/X/Y letters.</summary>
        Xbox,
        /// <summary>A PlayStation controller: cross, circle, square and triangle.</summary>
        PlayStation
    }
}
