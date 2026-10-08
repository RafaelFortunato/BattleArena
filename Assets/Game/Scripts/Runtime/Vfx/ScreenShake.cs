using Unity.Cinemachine;
using UnityEngine;

namespace BattleArena.Vfx
{
    /// <summary>
    /// Trauma-style camera shake on the follow camera. Events add trauma (0-1); the shake strength is trauma squared,
    /// so small knocks stay subtle while big ones stack into a real jolt, and trauma drains at a fixed rate.
    ///
    /// It runs on unscaled time so it keeps moving through a hit-stop, and sits still while the game is paused.
    /// The entry points are static and silent with no shake in the scene, like the audio ones.
    /// </summary>
    public sealed class ScreenShake : CinemachineExtension
    {
        [Tooltip("Camera-space offset at full trauma, in metres.")]
        [SerializeField, Min(0f)] private float maxOffset = 0.55f;
        [Tooltip("Camera roll at full trauma, in degrees.")]
        [SerializeField, Min(0f)] private float maxRoll = 1.2f;
        [Tooltip("How fast the shake wobbles.")]
        [SerializeField, Min(0f)] private float frequency = 24f;
        [Tooltip("Trauma lost per second; 1.6 clears a full-strength shake in about 0.6 s.")]
        [SerializeField, Min(0.01f)] private float decayPerSecond = 1.6f;
        [Tooltip("Shakes placed in the world fade out linearly with distance from the player, to nothing at this range.")]
        [SerializeField, Min(1f)] private float falloffDistance = 22f;

        private static ScreenShake _instance;

        private float _trauma;
        private float _lastTime;
        private float _seed;

        /// <summary>Adds trauma regardless of where it happened (the player being hit, the boss arriving).</summary>
        public static void Add(float trauma)
        {
            if (_instance != null)
                _instance._trauma = Mathf.Clamp01(_instance._trauma + trauma);
        }

        /// <summary>Adds trauma for something at <paramref name="position"/>, weaker the further it is from the player.</summary>
        public static void AddAt(Vector3 position, float trauma)
        {
            if (_instance == null)
                return;

            var target = _instance.ComponentOwner != null ? _instance.ComponentOwner.Follow : null;
            if (target != null)
            {
                var offset = position - target.position;
                offset.y = 0f;
                trauma *= 1f - Mathf.Clamp01(offset.magnitude / _instance.falloffDistance);
            }
            Add(trauma);
        }

        protected override void Awake()
        {
            base.Awake();
            _instance = this;
            _seed = Random.value * 100f;
            _lastTime = Time.unscaledTime;
        }

        protected override void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
            base.OnDestroy();
        }

        protected override void PostPipelineStageCallback(CinemachineVirtualCameraBase vcam, CinemachineCore.Stage stage,
            ref CameraState state, float deltaTime)
        {
            if (stage != CinemachineCore.Stage.Finalize)
                return;

            var now = Time.unscaledTime;
            var elapsed = now - _lastTime;
            _lastTime = now;
            // Paused: hold the trauma for when play resumes rather than shaking a frozen frame.
            if (Time.timeScale <= 0f || _trauma <= 0f)
                return;

            _trauma = Mathf.Max(0f, _trauma - decayPerSecond * elapsed);
            var shake = _trauma * _trauma;
            var t = now * frequency;
            var offset = new Vector3(Noise(t, 0f), Noise(t, 1f), 0f) * (maxOffset * shake);
            state.PositionCorrection += state.RawOrientation * offset;
            state.OrientationCorrection *= Quaternion.Euler(0f, 0f, Noise(t, 2f) * maxRoll * shake);
        }

        /// <summary>Smooth noise in -1..1, one independent channel per <paramref name="channel"/>.</summary>
        private float Noise(float t, float channel) => Mathf.PerlinNoise(_seed + channel * 17.3f, t) * 2f - 1f;
    }
}
