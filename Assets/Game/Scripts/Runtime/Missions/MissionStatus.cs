namespace BattleArena.Missions
{
    /// <summary>Outcome of a mission attempt so far.</summary>
    public enum MissionStatus
    {
        /// <summary>Still in progress.</summary>
        Running,
        /// <summary>The objective was met.</summary>
        Succeeded,
        /// <summary>Time ran out or the objective became impossible.</summary>
        Failed
    }
}
