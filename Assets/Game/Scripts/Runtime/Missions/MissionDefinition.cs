using UnityEngine;

namespace BattleArena.Missions
{
    /// <summary>
    /// Data for one kind of mission (a ScriptableObject), which creates a fresh <see cref="MissionInstance"/>
    /// holding the run-time state each time the mission starts. New missions are a definition + instance pair.
    /// </summary>
    public abstract class MissionDefinition : ScriptableObject
    {
        [SerializeField] private string title = "Mission";
        [SerializeField, TextArea] private string description;
        [SerializeField, Min(5f)] private float timeLimit = 40f;
        [Tooltip("Relative chance of being picked.")]
        [SerializeField, Min(0f)] private float weight = 1f;

        /// <summary>The mission's name.</summary>
        public string Title => title;

        /// <summary>What the player has to do. May contain a {0} placeholder that the instance fills in.</summary>
        public string Description => description;

        /// <summary>Seconds the player has to complete the mission.</summary>
        public float TimeLimit => timeLimit;

        /// <summary>Relative chance of this mission being picked.</summary>
        public float Weight => weight;

        /// <summary>Creates the run-time state for one attempt at this mission.</summary>
        public abstract MissionInstance CreateInstance(MissionContext context);
    }
}
