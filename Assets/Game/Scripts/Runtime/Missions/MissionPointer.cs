using UnityEngine;

namespace BattleArena.Missions
{
    /// <summary>
    /// Arrow on the ground next to the player pointing at the current mission objective.
    /// Hidden when there's no target or the target is already close.
    /// </summary>
    public sealed class MissionPointer : MonoBehaviour
    {
        [SerializeField] private Transform player;
        [SerializeField] private GameObject arrow;
        [SerializeField, Min(0f)] private float hideWithin = 5f;
        [SerializeField, Min(0f)] private float orbitDistance = 1.8f;
        [SerializeField, Min(0f)] private float heightOffset = 0.08f;

        private bool _hasTarget;
        private Vector3 _target;

        private void Awake() => arrow.SetActive(false);

        public void SetTarget(bool hasTarget, Vector3 target)
        {
            _hasTarget = hasTarget;
            _target = target;
            if (!hasTarget)
                arrow.SetActive(false);
        }

        // One object with trivial work; it has to follow the player every frame to read well.
        private void LateUpdate()
        {
            if (!_hasTarget)
                return;

            var origin = player.position;
            var toTarget = _target - origin;
            toTarget.y = 0f;
            var visible = toTarget.sqrMagnitude > hideWithin * hideWithin;
            if (arrow.activeSelf != visible)
                arrow.SetActive(visible);
            if (!visible)
                return;

            var direction = toTarget.normalized;
            transform.SetPositionAndRotation(origin + direction * orbitDistance + Vector3.up * heightOffset,
                Quaternion.LookRotation(direction));
        }
    }
}
