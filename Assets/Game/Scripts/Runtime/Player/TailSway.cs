using UnityEngine;

namespace BattleArena.Player
{
    /// <summary>
    /// Procedural husky tail: a slow side-to-side sway that ripples from base to tip, a little wider and quicker while
    /// running, and a lagging swing out against turns. Runs after the Animator and replaces what the clips do to the
    /// tail, so its rhythm doesn't follow the run cycle's short loop or its speed-scaled playback rate.
    /// </summary>
    public sealed class TailSway : MonoBehaviour
    {
        private const float TwoPi = 2f * Mathf.PI;

        [Tooltip("Tail bones from base to tip.")]
        [SerializeField] private Transform[] segments;
        [Tooltip("The character root: the sway turns about its up axis, and its turning drives the follow-through.")]
        [SerializeField] private Transform body;
        [SerializeField] private PlayerController controller;
        [Tooltip("Ground speed (m/s) at which the sway is fully in its running form.")]
        [SerializeField, Min(0.1f)] private float runSpeed = 4.5f;

        [Tooltip("Constant turn (degrees) per segment, base to tip, that brings the modelled tail round to sit behind the body.")]
        [SerializeField] private float[] centering = { -40f, -30f, -15f };

        [Header("Sway")]
        [Tooltip("Full left-right swings per second, standing and running.")]
        [SerializeField] private Vector2 frequency = new(0.45f, 0.9f);
        [Tooltip("Swing to each side (degrees) per unit of segment weight, standing and running.")]
        [SerializeField] private Vector2 amplitude = new(6f, 12f);
        [Tooltip("Each segment's share of the swing, base to tip. Segments stack, so the tip sweeps the furthest.")]
        [SerializeField] private float[] segmentWeights = { 0.8f, 1f, 1.2f };
        [Tooltip("How far (radians) each segment trails the one before it.")]
        [SerializeField, Range(0f, 2f)] private float segmentLag = 0.6f;
        [Tooltip("Up-down bob (degrees per unit of weight) while running, twice per swing.")]
        [SerializeField, Min(0f)] private float runBob = 1.5f;
        [Tooltip("How quickly the sway eases between its standing and running forms.")]
        [SerializeField, Min(0.1f)] private float blendRate = 3f;

        [Header("Follow-through")]
        [Tooltip("Degrees of swing per unit of weight for each degree/second the body turns. The tail trails the turn.")]
        [SerializeField, Min(0f)] private float turnSwing = 0.02f;
        [SerializeField, Min(0f)] private float maxTurnSwing = 10f;
        [Tooltip("Seconds the tail takes to settle after a turn.")]
        [SerializeField, Min(0.01f)] private float turnSettle = 0.2f;

        private Quaternion[] _rest;
        private float _phase;
        private float _blend;
        private float _turn;
        private float _turnVelocity;
        private float _lastYaw;

        private void Awake()
        {
            // The import pose is the tail's natural curl over the back; the sway is built on top of it.
            _rest = new Quaternion[segments.Length];
            for (var i = 0; i < segments.Length; i++)
                _rest[i] = segments[i].localRotation;
            _lastYaw = body.eulerAngles.y;
        }

        private void LateUpdate()
        {
            var deltaTime = Time.deltaTime;
            if (deltaTime > 0f)
                Advance(deltaTime);

            // Applied every frame, paused or not: the Animator rewrites the tail each frame and would otherwise show.
            var amplitudeNow = Mathf.Lerp(amplitude.x, amplitude.y, _blend);
            var bobNow = runBob * _blend;
            var up = body.up;
            var right = body.right;
            for (var i = 0; i < segments.Length; i++)
            {
                var weight = i < segmentWeights.Length ? segmentWeights[i] : 1f;
                var phase = _phase - i * segmentLag;
                var offset = i < centering.Length ? centering[i] : 0f;
                var swing = offset + weight * (amplitudeNow * Mathf.Sin(phase) + _turn);
                var bob = weight * bobNow * Mathf.Sin(2f * phase);

                // Parents first, so each segment's swing stacks on the one before it.
                var segment = segments[i];
                segment.localRotation = _rest[i];
                segment.rotation = Quaternion.AngleAxis(swing, up) * Quaternion.AngleAxis(bob, right) * segment.rotation;
            }
        }

        private void Advance(float deltaTime)
        {
            var running = Mathf.Clamp01(controller.CurrentSpeed / runSpeed);
            _blend = Mathf.Lerp(_blend, running, 1f - Mathf.Exp(-blendRate * deltaTime));

            // Advance a phase rather than evaluating sin(f * t): the frequency can then change without the tail jumping.
            _phase = Mathf.Repeat(_phase + TwoPi * Mathf.Lerp(frequency.x, frequency.y, _blend) * deltaTime, TwoPi);

            var yaw = body.eulerAngles.y;
            var yawRate = Mathf.DeltaAngle(_lastYaw, yaw) / deltaTime;
            _lastYaw = yaw;
            var turnTarget = Mathf.Clamp(-yawRate * turnSwing, -maxTurnSwing, maxTurnSwing);
            _turn = Mathf.SmoothDamp(_turn, turnTarget, ref _turnVelocity, turnSettle, Mathf.Infinity, deltaTime);
        }
    }
}
