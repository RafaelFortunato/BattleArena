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
        public static int DamageInRadius(Vector3 center, float radius, in DamageProfile damage, int layerMask)
        {
            var hits = 0;
            foreach (var hit in Overlap(center, radius, layerMask))
            {
                if (!hit.TryGetComponent(out IDamageable damageable) || !damageable.IsAlive)
                    continue;

                damage.ApplyTo(damageable);
                hits++;
            }
            return hits;
        }

        /// <summary>Slows and damages every living target in the radius. Returns how many were hit.</summary>
        /// <param name="chill">Frost slows show on the target; see <see cref="ISlowable.ApplySlow"/>.</param>
        public static int DamageAndSlowInRadius(Vector3 center, float radius, in DamageProfile damage, int layerMask,
            float slowMultiplier, float slowDuration, bool chill = false)
        {
            var hits = 0;
            foreach (var hit in Overlap(center, radius, layerMask))
            {
                if (!hit.TryGetComponent(out IDamageable damageable) || !damageable.IsAlive)
                    continue;

                // Slow before damaging: a killing blow despawns the enemy.
                if (hit.TryGetComponent(out ISlowable slowable))
                    slowable.ApplySlow(slowMultiplier, slowDuration, chill);
                damage.ApplyTo(damageable);
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

        /// <summary>Height above an enemy's feet that shots aim for, so shots from a raised muzzle still hit short enemies.</summary>
        public const float AimHeight = 0.9f;

        /// <summary>Direction from <paramref name="origin"/> to the body of an enemy standing at <paramref name="targetFeet"/>.</summary>
        public static Vector3 AimAt(Vector3 origin, Vector3 targetFeet)
        {
            var direction = targetFeet + Vector3.up * AimHeight - origin;
            return direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.forward;
        }
    }
}
