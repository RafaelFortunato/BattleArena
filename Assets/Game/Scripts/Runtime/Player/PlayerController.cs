using UnityEngine;

namespace BattleArena.Player
{
    /// <summary>Top-down WASD movement. Input arrives through events; Update only integrates motion.</summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerController : MonoBehaviour
    {
        private const float GroundedVerticalSpeed = -2f;

        [SerializeField] private InputReader input;
        [SerializeField, Min(0f)] private float moveSpeed = 6f;
        [SerializeField, Min(0f)] private float turnSpeed = 900f;
        [SerializeField] private float gravity = -25f;

        private CharacterController _controller;
        private Vector3 _moveDirection;
        private float _verticalSpeed;

        /// <summary>Planar direction the character is facing; used to aim spells when no target is nearby.</summary>
        public Vector3 Facing => transform.forward;

        private void Awake() => _controller = GetComponent<CharacterController>();

        private void OnEnable() => input.MoveChanged += OnMoveChanged;

        private void OnDisable()
        {
            input.MoveChanged -= OnMoveChanged;
            _moveDirection = Vector3.zero;
        }

        private void OnMoveChanged(Vector2 value) => _moveDirection = Vector3.ClampMagnitude(new Vector3(value.x, 0f, value.y), 1f);

        private void Update()
        {
            var isIdle = _moveDirection == Vector3.zero;
            if (isIdle && _controller.isGrounded)
                return;

            var deltaTime = Time.deltaTime;
            _verticalSpeed = _controller.isGrounded ? GroundedVerticalSpeed : _verticalSpeed + gravity * deltaTime;

            var velocity = _moveDirection * moveSpeed;
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
