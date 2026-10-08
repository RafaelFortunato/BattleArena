using BattleArena.Core;
using BattleArena.Pooling;
using BattleArena.Vfx;
using UnityEngine;

namespace BattleArena.Player
{
    /// <summary>
    /// Drives the character's Animator from movement: Idle when standing, Run while moving. The run cycle plays
    /// faster with movement speed (including Move Speed upgrades) so the steps keep up with the ground.
    ///
    /// Every spell cast also thrusts the staff: the Cast clip plays on an upper-body layer whose weight this fades in
    /// and out, so the legs keep running underneath, and a flare bursts from the crystal.
    /// </summary>
    public sealed class PlayerAnimator : MonoBehaviour
    {
        private static readonly int SpeedId = Animator.StringToHash("Speed");
        private static readonly int RunRateId = Animator.StringToHash("RunRate");
        private static readonly int CastState = Animator.StringToHash("Cast");
        private const int CastLayer = 1;

        [SerializeField] private Animator animator;
        [SerializeField] private PlayerController controller;
        [Tooltip("Ground speed (m/s) at which the run cycle plays at its authored rate.")]
        [SerializeField, Min(0.1f)] private float authoredRunSpeed = 4.5f;
        [SerializeField] private Vector2 runRateRange = new(0.8f, 1.8f);
        [SerializeField, Min(0f)] private float smoothing = 12f;

        [Header("Cast")]
        [Tooltip("Length of the Cast clip: the upper-body layer is faded out by its end.")]
        [SerializeField, Min(0.1f)] private float castDuration = 0.5f;
        [Tooltip("How fast the cast layer takes over, and how long it takes to hand back at the end.")]
        [SerializeField] private Vector2 castBlendInOut = new(0.05f, 0.15f);
        [Tooltip("Burst at the crystal when a spell goes off. Optional.")]
        [SerializeField] private FollowTarget castFlare;
        [SerializeField] private Transform muzzle;

        private float _speed;
        private float _castElapsed = float.PositiveInfinity;

        private void OnEnable() => EventBus<SpellCastEvent>.Subscribe(OnSpellCast);

        private void OnDisable() => EventBus<SpellCastEvent>.Unsubscribe(OnSpellCast);

        private void Update()
        {
            var deltaTime = Time.deltaTime;
            _speed = Mathf.Lerp(_speed, controller.CurrentSpeed, 1f - Mathf.Exp(-smoothing * deltaTime));
            animator.SetFloat(SpeedId, _speed);
            animator.SetFloat(RunRateId, Mathf.Clamp(_speed / authoredRunSpeed, runRateRange.x, runRateRange.y));

            if (_castElapsed > castDuration)
                return;
            _castElapsed += deltaTime;
            animator.SetLayerWeight(CastLayer, CastWeight(_castElapsed));
        }

        /// <summary>Quick in, hold through the thrust, ease out over the follow-through.</summary>
        private float CastWeight(float t)
        {
            if (t >= castDuration)
                return 0f;
            var fadeIn = Mathf.Clamp01(t / castBlendInOut.x);
            var fadeOut = Mathf.Clamp01((castDuration - t) / castBlendInOut.y);
            return Mathf.Min(fadeIn, fadeOut);
        }

        private void OnSpellCast(SpellCastEvent evt)
        {
            // Restart from the top even mid-cast: a quick double cast reads as two thrusts.
            animator.Play(CastState, CastLayer, 0f);
            _castElapsed = 0f;

            if (castFlare != null && muzzle != null)
                PoolManager.Spawn(castFlare, muzzle.position, Quaternion.identity).Follow(muzzle, Vector3.zero);
        }
    }
}
