using BattleArena.Combat;
using BattleArena.Pooling;
using BattleArena.Vfx;
using UnityEngine;

namespace BattleArena.Spells
{
    /// <summary>Freezing burst around the caster that damages and slows every enemy it touches.</summary>
    [CreateAssetMenu(fileName = "FrostNova", menuName = "BattleArena/Spells/Frost Nova")]
    public sealed class FrostNovaSpell : SpellDefinition
    {
        [SerializeField, Min(0.5f)] private float radius = 7f;
        [SerializeField, Range(0.05f, 1f)] private float slowMultiplier = 0.4f;
        [SerializeField, Min(0f)] private float slowDuration = 3f;
        [SerializeField] private ExpandingRing ringEffect;

        public override bool Cast(in SpellContext context)
        {
            var center = context.Caster.position;
            var damage = GetDamage(context);

            foreach (var hit in CombatQueries.Overlap(center, radius, context.TargetMask))
            {
                // Slow before damaging: a killing blow despawns the enemy.
                if (hit.TryGetComponent(out ISlowable slowable))
                    slowable.ApplySlow(slowMultiplier, slowDuration);
                if (hit.TryGetComponent(out IDamageable damageable))
                    damageable.TakeDamage(damage);
            }

            if (ringEffect != null)
                PoolManager.Spawn(ringEffect, center, Quaternion.identity).Play(radius);

            return true;
        }
    }
}
