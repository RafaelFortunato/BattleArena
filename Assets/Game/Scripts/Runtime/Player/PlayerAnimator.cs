using UnityEngine;

namespace BattleArena.Player
{
    /// <summary>
    /// Drives the character's Animator from movement: Idle when standing, Run while moving. The run cycle plays
    /// faster with movement speed (including Move Speed upgrades) so the steps keep up with the ground.
    /// </summary>
    public sealed class PlayerAnimator : MonoBehaviour
    {
        private static readonly int SpeedId = Animator.StringToHash("Speed");
        private static readonly int RunRateId = Animator.StringToHash("RunRate");

        [SerializeField] private Animator animator;
        [SerializeField] private PlayerController controller;
        [Tooltip("Ground speed (m/s) at which the run cycle plays at its authored rate.")]
        [SerializeField, Min(0.1f)] private float authoredRunSpeed = 4.5f;
        [SerializeField] private Vector2 runRateRange = new(0.8f, 1.8f);
        [SerializeField, Min(0f)] private float smoothing = 12f;

        private float _speed;

        private void Update()
        {
            _speed = Mathf.Lerp(_speed, controller.CurrentSpeed, 1f - Mathf.Exp(-smoothing * Time.deltaTime));
            animator.SetFloat(SpeedId, _speed);
            animator.SetFloat(RunRateId, Mathf.Clamp(_speed / authoredRunSpeed, runRateRange.x, runRateRange.y));
        }
    }
}
