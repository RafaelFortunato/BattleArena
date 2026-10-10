using UnityEngine;

namespace BattleArena.Missions
{
    /// <summary>Run-time state of one mission attempt. Ticked by <see cref="MissionDirector"/>.</summary>
    public abstract class MissionInstance
    {
        /// <summary>World access for the mission.</summary>
        protected readonly MissionContext Context;

        private float _endsAt;
        private string _text;
        private int _textA = int.MinValue;
        private int _textB = int.MinValue;

        /// <summary>Creates the state for one attempt; nothing happens until <see cref="Begin"/>.</summary>
        protected MissionInstance(MissionDefinition definition, MissionContext context)
        {
            Definition = definition;
            Context = context;
        }

        /// <summary>The mission this attempt is for.</summary>
        public MissionDefinition Definition { get; }

        /// <summary>
        /// The description shown when the mission starts. The definition's text by default; a mission can fill in
        /// numbers that are only known once it begins (e.g. a damage allowance from the player's max health).
        /// </summary>
        public virtual string Description => Definition.Description;

        /// <summary>Whether the attempt is running, succeeded or failed.</summary>
        public MissionStatus Status { get; private set; } = MissionStatus.Running;

        /// <summary>Seconds left before the attempt fails.</summary>
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

        /// <summary>Starts the clock and the mission's own setup.</summary>
        public void Begin()
        {
            _endsAt = Time.time + Definition.TimeLimit;
            OnBegin();
        }

        /// <summary>Advances a running attempt and fails it when time runs out.</summary>
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

        /// <summary>Sets the mission up: spawns its objects and subscribes to what it tracks.</summary>
        protected abstract void OnBegin();

        /// <summary>Per-tick work for missions that track something continuously.</summary>
        protected virtual void OnTick(float now, float deltaTime) { }

        /// <summary>Formats a one-number status line, rebuilding the string only when the number changes.</summary>
        protected string CachedText(string format, int value) => CachedText(format, value, 0);

        /// <summary>Formats a two-number status line, rebuilding the string only when the numbers change.</summary>
        protected string CachedText(string format, int a, int b)
        {
            if (_text == null || a != _textA || b != _textB)
            {
                _textA = a;
                _textB = b;
                _text = string.Format(format, a, b);
            }
            return _text;
        }

        /// <summary>Marks a running attempt as succeeded.</summary>
        protected void Succeed()
        {
            if (Status == MissionStatus.Running)
                Status = MissionStatus.Succeeded;
        }

        /// <summary>Marks a running attempt as failed.</summary>
        protected void Fail()
        {
            if (Status == MissionStatus.Running)
                Status = MissionStatus.Failed;
        }
    }
}
