using UnityEngine;

namespace BattleArena.Player
{
    /// <summary>Top-down WASD movement. Input arrives through events; Update only integrates motion.</summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerController : MonoBehaviour
    {
        private const float GroundedVerticalSpeed = -2f;

        [SerializeField] private InputReader input;
        [SerializeField] private PlayerStats stats;
        [SerializeField, Min(0f)] private float moveSpeed = 5.2f;
        [SerializeField, Min(0f)] private float turnSpeed = 900f;
        [SerializeField] private float gravity = -25f;

        private CharacterController _controller;
        private Vector3 _moveDirection;
        private float _verticalSpeed;

        /// <summary>Planar direction the character is facing; used to aim spells when no target is nearby.</summary>
        public Vector3 Facing => transform.forward;

        /// <summary>Current planar movement speed in metres per second (drives the run animation).</summary>
        public float CurrentSpeed { get; private set; }

        /// <summary>Movement speed at full stick, including upgrades.</summary>
        public float MaxSpeed => moveSpeed * stats.MoveSpeedMultiplier;

        private void Awake() => _controller = GetComponent<CharacterController>();

        private void OnEnable() => input.MoveChanged += OnMoveChanged;

        private void OnDisable()
        {
            input.MoveChanged -= OnMoveChanged;
            _moveDirection = Vector3.zero;
        }

        /// <summary>Instantly moves the character, e.g. for blink spells.</summary>
        public void Teleport(Vector3 position)
        {
            // CharacterController overrides transform changes while enabled.
            _controller.enabled = false;
            transform.position = position;
            _controller.enabled = true;
        }

        private void OnMoveChanged(Vector2 value) => _moveDirection = Vector3.ClampMagnitude(new Vector3(value.x, 0f, value.y), 1f);

        private void Update()
        {
            var isIdle = _moveDirection == Vector3.zero;
            CurrentSpeed = isIdle ? 0f : _moveDirection.magnitude * MaxSpeed;
            if (isIdle && _controller.isGrounded)
                return;

            var deltaTime = Time.deltaTime;
            _verticalSpeed = _controller.isGrounded ? GroundedVerticalSpeed : _verticalSpeed + gravity * deltaTime;

            var velocity = _moveDirection * MaxSpeed;
            velocity.y = _verticalSpeed;
            _controller.Move(velocity * deltaTime);

            if (_moveDirection.sqrMagnitude > 0.01f)
            {
                var targetRotation = Quaternion.LookRotation(_moveDirection);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, turnSpeed * deltaTime);
            }
        }
    }
}
