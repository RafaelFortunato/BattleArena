using BattleArena.Combat;
using BattleArena.Pooling;
using UnityEngine;

namespace BattleArena.Enemies
{
    /// <summary>Fires one or more projectiles at the player after a short wind-up (archers, wisps).</summary>
    public sealed class ProjectileAttack : TelegraphedAttack
    {
        [SerializeField] private Projectile projectilePrefab;
        [SerializeField] private Transform muzzle;
        [SerializeField, Min(1f)] private float projectileSpeed = 11f;
        [SerializeField, Min(1)] private int projectileCount = 1;
        [SerializeField, Range(0f, 60f)] private float spreadAngle = 18f;

        protected override void Execute(in EnemyTickContext context)
        {
            var origin = muzzle.position;
            var aim = CombatQueries.Planar(context.TargetPosition - origin);
            var damage = ScaledDamage(baseDamage);
            var firstAngle = -spreadAngle * (projectileCount - 1) * 0.5f;

            for (var i = 0; i < projectileCount; i++)
            {
                var direction = Quaternion.AngleAxis(firstAngle + spreadAngle * i, Vector3.up) * aim;
                var projectile = PoolManager.Spawn(projectilePrefab, origin, Quaternion.LookRotation(direction));
                projectile.Launch(direction, projectileSpeed, damage);
            }
        }
    }
}
