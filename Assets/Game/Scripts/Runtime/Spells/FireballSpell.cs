using BattleArena.Combat;
using BattleArena.Pooling;
using UnityEngine;

namespace BattleArena.Spells
{
    /// <summary>Hurls a fireball at the nearest enemy (or forward) that explodes on impact.</summary>
    [CreateAssetMenu(fileName = "Fireball", menuName = "BattleArena/Spells/Fireball")]
    public sealed class FireballSpell : SpellDefinition
    {
        [SerializeField] private Projectile projectilePrefab;
        [SerializeField, Min(1f)] private float speed = 16f;
        [SerializeField, Min(0.5f)] private float explosionRadius = 3.5f;
        [SerializeField, Min(1f)] private float targetingRange = 18f;

        public override bool Cast(in SpellContext context)
        {
            var direction = CombatQueries.TryFindNearest(context.Origin, targetingRange, context.TargetMask, out var target)
                ? CombatQueries.Planar(target.position - context.Origin)
                : context.AimDirection;

            var fireball = PoolManager.Spawn(projectilePrefab, context.Origin, Quaternion.LookRotation(direction));
            fireball.Launch(direction, speed, GetDamage(context), explosionRadius: explosionRadius, explosionMask: context.TargetMask);
            return true;
        }
    }
}
