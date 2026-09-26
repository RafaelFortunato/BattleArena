using System.Collections.Generic;
using BattleArena.Combat;
using BattleArena.Pooling;
using BattleArena.Vfx;
using UnityEngine;

namespace BattleArena.Spells
{
    /// <summary>Lightning that strikes the nearest enemy, then jumps to nearby enemies with decaying damage.</summary>
    [CreateAssetMenu(fileName = "ChainLightning", menuName = "BattleArena/Spells/Chain Lightning")]
    public sealed class ChainLightningSpell : SpellDefinition
    {
        private const int MaxTargetsLimit = 16;

        [SerializeField, Min(1f)] private float range = 14f;
        [SerializeField, Min(1f)] private float jumpRadius = 7f;
        [SerializeField, Range(1, MaxTargetsLimit)] private int maxTargets = 6;
        [SerializeField, Range(0.1f, 1f)] private float damageFalloff = 0.85f;
        [SerializeField] private LightningArc arcEffect;
        [SerializeField] private TimedRelease hitEffect;

        // Shared scratch buffers: spells are cast one at a time on the main thread.
        private static readonly Vector3[] Points = new Vector3[MaxTargetsLimit + 1];
        private static readonly List<IDamageable> Targets = new(MaxTargetsLimit);
        private static readonly HashSet<Collider> Visited = new();

        public override bool Cast(in SpellContext context)
        {
            if (!CombatQueries.TryFindNearest(context.Origin, range, context.TargetMask, out var first))
                return false;

            // Pierce and bounce upgrades add extra jumps; Area widens the jump range.
            var targets = Mathf.Min(MaxTargetsLimit, maxTargets + context.Modifiers.ExtraPierce + context.Modifiers.ExtraBounces);
            CollectChain(first, context.TargetMask, targets, context.Area(jumpRadius));

            // Damage after collecting: a kill despawns the enemy, which would break the chain search.
            var damage = GetDamage(context);
            for (var i = 0; i < Targets.Count; i++)
            {
                damage.Scaled(Mathf.Pow(damageFalloff, i)).ApplyTo(Targets[i]);
                if (hitEffect != null)
                    PoolManager.Spawn(hitEffect, Points[i + 1], Quaternion.identity);
            }

            Points[0] = context.Origin;
            if (arcEffect != null)
                PoolManager.Spawn(arcEffect, context.Origin, Quaternion.identity).Show(Points, Targets.Count + 1);

            return true;
        }

        private static void CollectChain(Transform first, int targetMask, int maxTargets, float jumpRadius)
        {
            Targets.Clear();
            Visited.Clear();

            var current = first.GetComponent<Collider>();
            while (current != null && Targets.Count < maxTargets)
            {
                Visited.Add(current);
                if (current.TryGetComponent(out IDamageable damageable))
                {
                    Targets.Add(damageable);
                    Points[Targets.Count] = current.bounds.center;
                }

                current = FindNextTarget(current.transform.position, targetMask, jumpRadius);
            }
        }

        private static Collider FindNextTarget(Vector3 from, int targetMask, float jumpRadius)
        {
            Collider best = null;
            var bestSqrDistance = float.MaxValue;
            foreach (var hit in CombatQueries.Overlap(from, jumpRadius, targetMask))
            {
                if (Visited.Contains(hit) || !hit.TryGetComponent(out IDamageable damageable) || !damageable.IsAlive)
                    continue;

                var sqrDistance = (hit.transform.position - from).sqrMagnitude;
                if (sqrDistance < bestSqrDistance)
                {
                    bestSqrDistance = sqrDistance;
                    best = hit;
                }
            }
            return best;
        }
    }
}
