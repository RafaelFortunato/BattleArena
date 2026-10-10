using System.Collections.Generic;
using UnityEngine;

namespace BattleArena.Enemies.Movement
{
    /// <summary>
    /// Flies straight at the player, ignoring the NavMesh, faster than the player can run but with a
    /// very limited turn rate, so it overshoots and has to loop back. The prefab's NavMeshAgent is disabled.
    /// Motion is integrated every frame by <see cref="EnemyDirector"/> for all flyers at once (smooth
    /// flight without one Update per enemy).
    /// </summary>
    public sealed class FlyingMovement : EnemyMovement
    {
        private static readonly List<FlyingMovement> ActiveFlyers = new(32);

        [SerializeField, Min(1f)] private float turnRateDegrees = 75f;
        [Tooltip("Flyers are kept inside this square around the arena center.")]
        [SerializeField, Min(1f)] private float arenaHalfExtent = 39f;

        private Vector3 _heading;
        private int _index = -1;

        /// <summary>Every live flyer, moved each frame by the director.</summary>
        internal static IReadOnlyList<FlyingMovement> Active => ActiveFlyers;

        /// <summary>Empties the flyer list before play starts.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRegistry() => ActiveFlyers.Clear();

        /// <summary>Starts flying the way it faces and joins the flyer list.</summary>
        public override void OnSpawned()
        {
            _heading = transform.forward;
            if (_index < 0)
            {
                _index = ActiveFlyers.Count;
                ActiveFlyers.Add(this);
            }
        }

        /// <summary>Leaves the flyer list.</summary>
        public override void OnDespawned() => Unregister();

        /// <summary>Leaves the flyer list. Also covers scene unload, where OnDespawned is not called.</summary>
        private void OnDisable() => Unregister();

        /// <summary>Nothing to do: flight is integrated every frame by <see cref="Integrate"/>.</summary>
        public override void Tick(in EnemyTickContext context) { }

        /// <summary>Steers toward <paramref name="target"/> with a capped turn rate and moves forward.</summary>
        internal void Integrate(Vector3 target, float deltaTime)
        {
            if (Owner.IsHeld)
                return;

            var position = transform.position;
            var desired = Planar(target - position);
            if (desired.sqrMagnitude > 0.01f)
                _heading = Vector3.RotateTowards(_heading, desired.normalized, turnRateDegrees * Mathf.Deg2Rad * deltaTime, 0f);

            position += _heading * (Owner.CurrentSpeed * deltaTime);
            position.x = Mathf.Clamp(position.x, -arenaHalfExtent, arenaHalfExtent);
            position.z = Mathf.Clamp(position.z, -arenaHalfExtent, arenaHalfExtent);
            transform.SetPositionAndRotation(position, Quaternion.LookRotation(_heading));
        }

        /// <summary>Swap-removes this flyer from the list, fixing up the index of the one moved.</summary>
        private void Unregister()
        {
            if (_index < 0)
                return;

            // Swap-remove, fixing up the moved flyer's index.
            var last = ActiveFlyers.Count - 1;
            var moved = ActiveFlyers[last];
            ActiveFlyers[_index] = moved;
            moved._index = _index;
            ActiveFlyers.RemoveAt(last);
            _index = -1;
        }
    }
}
