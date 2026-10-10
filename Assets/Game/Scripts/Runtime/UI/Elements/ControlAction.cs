namespace BattleArena.UI.Elements
{
    /// <summary>Something the player can do, as shown in a button prompt.</summary>
    public enum ControlAction
    {
        /// <summary>Move the character.</summary>
        Move,
        /// <summary>All three spell buttons in a row.</summary>
        Spells,
        /// <summary>Cast the first spell.</summary>
        Spell1,
        /// <summary>Cast the second spell.</summary>
        Spell2,
        /// <summary>Cast the third spell.</summary>
        Spell3,
        /// <summary>Use a well or chest.</summary>
        Interact,
        /// <summary>Open the pause menu.</summary>
        Pause,
        /// <summary>Leave a menu: Esc on a keyboard, B / Circle on a gamepad.</summary>
        Back,
        /// <summary>Move between level-up cards.</summary>
        Browse,
        /// <summary>Pick the highlighted card.</summary>
        Confirm
    }
}
