using BattleArena.Pooling;
using UnityEngine;

namespace BattleArena.Vfx
{
    /// <summary>Keeps a pooled effect on a moving target (e.g. a shock marker on an enemy) until the target disappears.</summary>
    public sealed class FollowTarget : MonoBehaviour, IPoolable
    {
        private Transform _target;
        private Vector3 _offset;

        public void Follow(Transform target, Vector3 offset)
        {
            _target = target;
            _offset = offset;
        }

        public void OnSpawned() => _target = null;

        public void OnDespawned() => _target = null;

        private void LateUpdate()
        {
            // Pooled enemies are deactivated on death; stay where they fell rather than following a respawn.
            if (_target == null || !_target.gameObject.activeInHierarchy)
            {
                _target = null;
                return;
            }
            transform.position = _target.position + _offset;
        }
    }
}
