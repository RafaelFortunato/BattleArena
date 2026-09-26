using BattleArena.Combat;
using BattleArena.Pooling;
using UnityEngine;

namespace BattleArena.Spells
{
    /// <summary>
    /// Fires one or more projectiles at the nearest enemy (or forward). Configured as Fireball
    /// (exploding) and Arcane Lance (piercing).
    /// </summary>
    [CreateAssetMenu(fileName = "ProjectileSpell", menuName = "BattleArena/Spells/Projectile Spell")]
    public sealed class ProjectileSpell : SpellDefinition
    {
        [SerializeField] private Projectile projectilePrefab;
        [SerializeField, Min(1f)] private float speed = 16f;
        [SerializeField, Min(1)] private int projectileCount = 1;
        [SerializeField, Range(0f, 45f)] private float spreadAngle = 10f;
        [Tooltip("Extra enemies each projectile passes through.")]
        [SerializeField, Min(0)] private int pierce;
        [Tooltip("When above zero, projectiles explode on impact.")]
        [SerializeField, Min(0f)] private float explosionRadius;
        [SerializeField, Min(1f)] private float targetingRange = 18f;

        public override bool Cast(in SpellContext context)
        {
            var aim = CombatQueries.TryFindNearest(context.Origin, targetingRange, context.TargetMask, out var target)
                ? CombatQueries.Planar(target.position - context.Origin)
                : context.Caster.Facing;

            var damage = GetDamage(context);
            var count = projectileCount + context.Modifiers.ExtraProjectiles;
            var totalPierce = pierce + context.Modifiers.ExtraPierce;
            var radius = context.Area(explosionRadius);
            var firstAngle = -spreadAngle * (count - 1) * 0.5f;
            for (var i = 0; i < count; i++)
            {
                var direction = Quaternion.AngleAxis(firstAngle + spreadAngle * i, Vector3.up) * aim;
                var projectile = PoolManager.Spawn(projectilePrefab, context.Origin, Quaternion.LookRotation(direction));
                projectile.Launch(direction, speed, damage, totalPierce, radius, context.TargetMask);
            }

            return true;
        }
    }
}
