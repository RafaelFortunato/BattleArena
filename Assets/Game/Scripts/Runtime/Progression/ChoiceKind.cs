namespace BattleArena.Progression
{
    /// <summary>Why a card choice is being shown.</summary>
    public enum ChoiceKind
    {
        /// <summary>A level-up with stat cards.</summary>
        LevelUp,
        /// <summary>A level-up that opens a spell slot, with spell cards.</summary>
        NewSpell,
        /// <summary>A completed mission, with spell upgrade cards.</summary>
        MissionReward,
        /// <summary>An opened treasure chest, with stat cards.</summary>
        Treasure
    }
}
