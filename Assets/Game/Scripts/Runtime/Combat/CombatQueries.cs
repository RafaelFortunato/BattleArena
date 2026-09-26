using System;
using UnityEngine;

namespace BattleArena.Combat
{
    /// <summary>Allocation-free physics queries shared by attacks, spells and the boss.</summary>
    public static class CombatQueries
    {
        private const int BufferSize = 256;
        private static readonly Collider[] Buffer = new Collider[BufferSize];

        /// <summary>
        /// Colliders overlapping the sphere. The span points into a shared buffer:
        /// consume it before running another query.
        /// </summary>
        public static ReadOnlySpan<Collider> Overlap(Vector3 center, float radius, int layerMask)
        {
            var count = Physics.OverlapSphereNonAlloc(center, radius, Buffer, layerMask, QueryTriggerInteraction.Collide);
            return new ReadOnlySpan<Collider>(Buffer, 0, count);
        }

        public static bool TryFindNearest(Vector3 origin, float radius, int layerMask, out Transform nearest)
        {
            nearest = null;
            var bestSqrDistance = float.MaxValue;

            foreach (var hit in Overlap(origin, radius, layerMask))
            {
                if (!hit.TryGetComponent(out IDamageable damageable) || !damageable.IsAlive)
                    continue;

                var sqrDistance = (hit.transform.position - origin).sqrMagnitude;
                if (sqrDistance < bestSqrDistance)
                {
                    bestSqrDistance = sqrDistance;
                    nearest = hit.transform;
                }
            }

            return nearest != null;
        }

        /// <summary>Damages every living target in the radius and returns how many were hit.</summary>
        public static int DamageInRadius(Vector3 center, float radius, float damage, int layerMask)
        {
            var hits = 0;
            foreach (var hit in Overlap(center, radius, layerMask))
            {
                if (!hit.TryGetComponent(out IDamageable damageable) || !damageable.IsAlive)
                    continue;

                damageable.TakeDamage(damage);
                hits++;
            }
            return hits;
        }

        /// <summary>Flattens a direction onto the ground plane.</summary>
        public static Vector3 Planar(Vector3 direction)
        {
            direction.y = 0f;
            return direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.forward;
        }
    }
}
