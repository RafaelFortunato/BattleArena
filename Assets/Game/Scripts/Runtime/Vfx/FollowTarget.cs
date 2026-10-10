using BattleArena.Pooling;
using UnityEngine;

namespace BattleArena.Vfx
{
    /// <summary>Keeps a pooled effect on a moving target (e.g. a shock marker on an enemy) until the target disappears.</summary>
    public sealed class FollowTarget : MonoBehaviour, IPoolable
    {
        private Transform _target;
        private Vector3 _offset;

        /// <summary>Keeps the effect at <paramref name="offset"/> from <paramref name="target"/>.</summary>
        public void Follow(Transform target, Vector3 offset)
        {
            _target = target;
            _offset = offset;
        }

        /// <summary>Starts without a target.</summary>
        public void OnSpawned() => _target = null;

        /// <summary>Forgets the target.</summary>
        public void OnDespawned() => _target = null;

        /// <summary>
        /// Follows the target; once it is gone or deactivated (pooled enemies are, on death), the effect stays where it
        /// fell rather than following a respawn.
        /// </summary>
        private void LateUpdate()
        {
            if (_target == null || !_target.gameObject.activeInHierarchy)
            {
                _target = null;
                return;
            }
            transform.position = _target.position + _offset;
        }
    }
}
