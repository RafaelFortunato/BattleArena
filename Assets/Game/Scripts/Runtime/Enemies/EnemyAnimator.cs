using BattleArena.Pooling;
using UnityEngine;

namespace BattleArena.Enemies
{
    /// <summary>
    /// Drives an enemy's Animator: Idle/Move from how fast it actually travels (so it also covers flyers and
    /// dashes) and an Attack swing timed to land when the attack does. Once the blow has landed and the enemy moves
    /// again, the follow-through blends straight into the walk so it never slides. Hits play no reaction: nothing
    /// stuns enemies.
    /// </summary>
    public sealed class EnemyAnimator : MonoBehaviour, IPoolable
    {
        private static readonly int SpeedId = Animator.StringToHash("Speed");
        private static readonly int MoveRateId = Animator.StringToHash("MoveRate");
        private static readonly int AttackRateId = Animator.StringToHash("AttackRate");
        private static readonly int AttackId = Animator.StringToHash("Attack");
        private static readonly int IdleState = Animator.StringToHash("Idle");
        private static readonly int MoveState = Animator.StringToHash("Move");
        private static readonly int AttackTag = Animator.StringToHash("Attack");
        /// <summary>Matches the controller's Idle → Move threshold.</summary>
        private const float MovingSpeed = 0.35f;
        private const float SwingToMoveBlend = 0.15f;

        [SerializeField] private Animator animator;
        [Tooltip("Travel speed (m/s) at which the move clip plays at its authored rate.")]
        [SerializeField, Min(0.1f)] private float referenceSpeed = 3f;
        [Tooltip("Length of the attack clip; used to speed it up so the blow lands at the end of the wind-up.")]
        [SerializeField, Min(0.05f)] private float attackClipLength = 0.8f;
        [Tooltip("Fraction of the attack clip at which the blow connects.")]
        [SerializeField, Range(0.1f, 1f)] private float attackImpactPoint = 0.55f;

        private Enemy _enemy;
        private Vector3 _lastPosition;
        private float _speed;

        private void Awake()
        {
            _enemy = GetComponent<Enemy>();
            _enemy.AttackStarted += OnAttackStarted;
        }

        private void OnDestroy() => _enemy.AttackStarted -= OnAttackStarted;

        public void OnSpawned()
        {
            _lastPosition = transform.position;
            _speed = 0f;
            // Desynchronise idles so a freshly spawned group doesn't move in lockstep.
            animator.Play(IdleState, 0, Random.value);
        }

        public void OnDespawned() { }

        private void Update()
        {
            var deltaTime = Time.deltaTime;
            if (deltaTime <= 0f)
                return;

            var position = transform.position;
            var travelled = position - _lastPosition;
            travelled.y = 0f;
            _lastPosition = position;

            var speed = travelled.magnitude / deltaTime;
            _speed = Mathf.Lerp(_speed, speed, 1f - Mathf.Exp(-10f * deltaTime));
            animator.SetFloat(SpeedId, _speed);
            animator.SetFloat(MoveRateId, Mathf.Clamp(_speed / referenceSpeed, 0.6f, 2.2f));

            if (_speed > MovingSpeed && !animator.IsInTransition(0))
            {
                var state = animator.GetCurrentAnimatorStateInfo(0);
                if (state.tagHash == AttackTag && state.normalizedTime >= attackImpactPoint)
                    animator.CrossFadeInFixedTime(MoveState, SwingToMoveBlend);
            }
        }

        private void OnAttackStarted(float secondsToImpact)
        {
            // Play the swing so its impact frame lines up with the moment the attack lands.
            var rate = secondsToImpact > 0.05f ? attackClipLength * attackImpactPoint / secondsToImpact : 1.4f;
            animator.SetFloat(AttackRateId, Mathf.Clamp(rate, 0.5f, 2.5f));
            animator.SetTrigger(AttackId);
        }
    }
}
