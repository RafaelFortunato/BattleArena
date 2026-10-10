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
        [Tooltip("Passes through every enemy (walls still stop it); the Pierce stat then has nothing to add.")]
        [SerializeField] private bool infinitePierce;
        [Tooltip("When above zero, projectiles explode on impact, and again on each enemy Pierce carries them through.")]
        [SerializeField, Min(0f)] private float explosionRadius;
        [SerializeField, Min(1f)] private float targetingRange = 18f;
        [Tooltip("When above zero, projectiles fly level at this height above the caster's feet instead of aiming down " +
                 "from the cast origin, so long-range shots never dip under the floor.")]
        [SerializeField, Min(0f)] private float flightHeight;

        /// <summary>
        /// Always more projectiles; Area only when it explodes; Pierce only when it does not already pierce everything.
        /// </summary>
        public override SpellStats ScalesWith =>
            SpellStats.Projectiles
            | (explosionRadius > 0f ? SpellStats.Area : SpellStats.None)
            | (infinitePierce ? SpellStats.None : SpellStats.Pierce);

        /// <summary>
        /// Fires a fan of projectiles at the nearest enemy (or forward), flying level when a flight height is set.
        /// </summary>
        public override bool Cast(in SpellContext context)
        {
            var origin = context.Origin;
            if (flightHeight > 0f)
                origin.y = context.Caster.Transform.position.y + flightHeight;

            var aim = CombatQueries.TryFindNearest(origin, targetingRange, context.TargetMask, out var target)
                ? CombatQueries.AimAt(origin, target.position)
                : context.Caster.Facing;
            if (flightHeight > 0f)
                aim = Flatten(aim, context.Caster.Facing);

            var damage = GetDamage(context);
            var count = projectileCount + context.Modifiers.ExtraProjectiles;
            var totalPierce = infinitePierce ? int.MaxValue : pierce + context.Modifiers.ExtraPierce;
            var radius = context.Area(explosionRadius);
            // A level flyer's blasts go off on the floor under it, not at flight height.
            var groundHeight = flightHeight > 0f ? context.Caster.Transform.position.y : float.NegativeInfinity;
            var firstAngle = -spreadAngle * (count - 1) * 0.5f;
            for (var i = 0; i < count; i++)
            {
                var direction = Quaternion.AngleAxis(firstAngle + spreadAngle * i, Vector3.up) * aim;
                var projectile = PoolManager.Spawn(projectilePrefab, origin, Quaternion.LookRotation(direction));
                projectile.Launch(direction, speed, damage, totalPierce, radius, context.TargetMask, groundHeight);
            }

            return true;
        }

        /// <summary>
        /// <paramref name="direction"/> on the ground plane, or <paramref name="fallback"/> when it points straight up
        /// or down.
        /// </summary>
        private static Vector3 Flatten(Vector3 direction, Vector3 fallback)
        {
            direction.y = 0f;
            return direction.sqrMagnitude > 0.0001f ? direction.normalized : fallback;
        }
    }
}
