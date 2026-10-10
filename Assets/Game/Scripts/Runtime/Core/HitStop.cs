using UnityEngine;

namespace BattleArena.Core
{
    /// <summary>
    /// A split-second near-freeze on a heavy blow, so it lands with weight. It only ever starts from normal play: the
    /// pause, level-up and end-of-match screens own <see cref="Time.timeScale"/>, so it never starts over them, and if
    /// one of them changes the time scale mid-freeze, it leaves their value alone when it ends.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class HitStop : Singleton<HitStop>
    {
        [Tooltip("Time scale during the freeze. Not quite zero, so animation and effects creep rather than stop dead.")]
        [SerializeField, Range(0f, 0.5f)] private float frozenScale = 0.05f;

        private bool _active;
        private float _until;

        /// <summary>Freezes play for <paramref name="seconds"/> of real time; overlapping calls extend the freeze.</summary>
        public static void Trigger(float seconds)
        {
            if (Instance != null && seconds > 0f)
                Instance.Begin(seconds);
        }

        /// <summary>Starts or extends the freeze; a freeze only starts from normal speed.</summary>
        private void Begin(float seconds)
        {
            var until = Time.unscaledTime + seconds;
            if (_active)
            {
                _until = Mathf.Max(_until, until);
                return;
            }

            if (!Mathf.Approximately(Time.timeScale, 1f))
                return;

            _active = true;
            _until = until;
            Time.timeScale = frozenScale;
        }

        /// <summary>
        /// Restores normal speed when the freeze ends, unless another system changed the time scale meanwhile.
        /// </summary>
        private void Update()
        {
            if (!_active || Time.unscaledTime < _until)
                return;

            _active = false;
            // Someone else took over the clock mid-freeze (a level-up, the end screen); theirs stands.
            if (Mathf.Approximately(Time.timeScale, frozenScale))
                Time.timeScale = 1f;
        }

        /// <summary>Restores normal speed if destroyed mid-freeze.</summary>
        protected override void OnDestroy()
        {
            if (_active && Mathf.Approximately(Time.timeScale, frozenScale))
                Time.timeScale = 1f;
            base.OnDestroy();
        }
    }
}
