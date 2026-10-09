using BattleArena.Combat;
using BattleArena.Core;
using BattleArena.Enemies;
using UnityEngine;

namespace BattleArena.Missions
{
    public enum MissionStatus
    {
        Running,
        Succeeded,
        Failed
    }

    /// <summary>World access a running mission needs.</summary>
    public sealed class MissionContext
    {
        public readonly Transform Player;
        public readonly Health PlayerHealth;
        public readonly EnemySpawner Spawner;
        private readonly GameManager _game;

        public MissionContext(Transform player, Health playerHealth, EnemySpawner spawner, GameManager game)
        {
            Player = player;
            PlayerHealth = playerHealth;
            Spawner = spawner;
            _game = game;
        }

        /// <summary>Minutes since the match started.</summary>
        public float ElapsedMinutes => _game.ElapsedTime / 60f;
    }

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

        public string Title => title;
        public string Description => description;
        public float TimeLimit => timeLimit;
        public float Weight => weight;

        public abstract MissionInstance CreateInstance(MissionContext context);
    }

    /// <summary>Run-time state of one mission attempt. Ticked by <see cref="MissionDirector"/>.</summary>
    public abstract class MissionInstance
    {
        protected readonly MissionContext Context;
        private float _endsAt;
        private string _text;
        private int _textA = int.MinValue;
        private int _textB = int.MinValue;
        private int _textC = int.MinValue;

        protected MissionInstance(MissionDefinition definition, MissionContext context)
        {
            Definition = definition;
            Context = context;
        }

        public MissionDefinition Definition { get; }

        /// <summary>
        /// The description shown when the mission starts. The definition's text by default; a mission can fill in
        /// numbers that are only known once it begins (e.g. a damage allowance from the player's max health).
        /// </summary>
        public virtual string Description => Definition.Description;
        public MissionStatus Status { get; private set; } = MissionStatus.Running;
        public float TimeRemaining => Mathf.Max(0f, _endsAt - Time.time);

        /// <summary>0..1 completion shown on the HUD.</summary>
        public abstract float Progress { get; }

        /// <summary>Short status line, e.g. "Shards 1 / 3".</summary>
        public abstract string ProgressText { get; }

        /// <summary>Where the objective pointer should aim, if the mission has a location.</summary>
        public virtual bool TryGetTarget(out Vector3 position)
        {
            position = default;
            return false;
        }

        public void Begin()
        {
            _endsAt = Time.time + Definition.TimeLimit;
            OnBegin();
        }

        public void Tick(float now, float deltaTime)
        {
            if (Status != MissionStatus.Running)
                return;

            OnTick(now, deltaTime);
            if (Status == MissionStatus.Running && now >= _endsAt)
                Fail();
        }

        /// <summary>Removes everything the mission spawned or subscribed to. Called once when it ends.</summary>
        public abstract void Cleanup();

        protected abstract void OnBegin();

        protected virtual void OnTick(float now, float deltaTime) { }

        /// <summary>Formats a two-number status line, rebuilding the string only when the numbers change.</summary>
        protected string CachedText(string format, int a, int b) => CachedText(format, a, b, 0);

        /// <summary>Formats a status line of up to three numbers, rebuilding the string only when they change.</summary>
        protected string CachedText(string format, int a, int b, int c)
        {
            if (_text == null || a != _textA || b != _textB || c != _textC)
            {
                _textA = a;
                _textB = b;
                _textC = c;
                _text = string.Format(format, a, b, c);
            }
            return _text;
        }

        protected void Succeed()
        {
            if (Status == MissionStatus.Running)
                Status = MissionStatus.Succeeded;
        }

        protected void Fail()
        {
            if (Status == MissionStatus.Running)
                Status = MissionStatus.Failed;
        }
    }
}
