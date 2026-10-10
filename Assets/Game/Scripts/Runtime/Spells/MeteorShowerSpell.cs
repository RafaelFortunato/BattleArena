using BattleArena.Combat;
using BattleArena.Pooling;
using UnityEngine;
using UnityEngine.AI;

namespace BattleArena.Spells
{
    /// <summary>Calls down a staggered volley of meteors on enemies around the caster.</summary>
    [CreateAssetMenu(fileName = "MeteorShower", menuName = "BattleArena/Spells/Meteor Shower")]
    public sealed class MeteorShowerSpell : SpellDefinition
    {
        [SerializeField] private DelayedAreaStrike meteorPrefab;
        [SerializeField, Min(1)] private int meteorCount = 6;
        [Tooltip("Meteors added per Split Cast pick. Shared projectile stats (Multishot) still add one meteor each.")]
        [SerializeField, Min(1)] private int meteorsPerProjectileUpgrade = 2;
        [SerializeField, Min(0.5f)] private float impactRadius = 3f;
        [SerializeField, Min(1f)] private float areaRadius = 12f;
        [SerializeField, Min(0.1f)] private float firstImpactDelay = 0.8f;
        [SerializeField, Min(0f)] private float delayBetweenImpacts = 0.15f;

        /// <summary>Area is each impact's size; Projectiles adds meteors.</summary>
        public override SpellStats ScalesWith => SpellStats.Area | SpellStats.Projectiles;

        /// <summary>A Split Cast pick adds several meteors, since one meteor is a small step.</summary>
        public override float UpgradeScale(SpellUpgradeKind kind) =>
            kind == SpellUpgradeKind.ExtraProjectiles ? meteorsPerProjectileUpgrade : 1f;

        /// <summary>
        /// Drops staggered meteors on the enemies around the caster, filling any remaining meteors with random points
        /// in the area.
        /// </summary>
        public override bool Cast(in SpellContext context)
        {
            var center = context.CasterPosition;
            var damage = GetDamage(context);
            var count = meteorCount + context.Modifiers.ExtraProjectiles;
            var impact = context.Area(impactRadius);

            var targets = CombatQueries.Overlap(center, areaRadius, context.TargetMask);
            for (var i = 0; i < count; i++)
            {
                var position = i < targets.Length
                    ? targets[i].transform.position
                    : RandomGroundPoint(center);
                position.y = center.y;

                var meteor = PoolManager.Spawn(meteorPrefab, position, Quaternion.identity);
                meteor.Arm(impact, firstImpactDelay + delayBetweenImpacts * i, damage, context.TargetMask);
            }

            return true;
        }

        /// <summary>A random walkable point within the shower's area.</summary>
        private Vector3 RandomGroundPoint(Vector3 center)
        {
            var offset = Random.insideUnitCircle * areaRadius;
            var candidate = center + new Vector3(offset.x, 0f, offset.y);
            return NavMesh.SamplePosition(candidate, out var hit, 2f, NavMesh.AllAreas) ? hit.position : center;
        }
    }
}
