using BattleArena.Combat;
using BattleArena.Pooling;
using BattleArena.Vfx;
using UnityEngine;
using UnityEngine.AI;

namespace BattleArena.Spells
{
    /// <summary>Blinks forward, becoming briefly invulnerable and blasting enemies at the destination.</summary>
    [CreateAssetMenu(fileName = "PhaseStep", menuName = "BattleArena/Spells/Phase Step")]
    public sealed class PhaseStepSpell : SpellDefinition
    {
        [SerializeField, Min(1f)] private float distance = 7f;
        [SerializeField, Min(0.5f)] private float blastRadius = 3.5f;
        [SerializeField, Min(0f)] private float invulnerabilityDuration = 0.6f;
        [SerializeField] private TimedRelease departEffect;
        [SerializeField] private ExpandingRing arrivalRing;

        public override bool Cast(in SpellContext context)
        {
            var start = context.CasterPosition;
            var desired = start + context.Caster.Facing * distance;

            // Stop at walls and obstacles: NavMesh.Raycast reports where the straight path leaves the mesh.
            var destination = NavMesh.Raycast(start, desired, out var hit, NavMesh.AllAreas) ? hit.position : desired;
            if (!NavMesh.SamplePosition(destination, out var sample, 1.5f, NavMesh.AllAreas))
                return false;
            destination = sample.position;

            if (departEffect != null)
                PoolManager.Spawn(departEffect, start + Vector3.up, Quaternion.identity);

            context.Caster.Teleport(destination);
            context.Caster.Health.GrantInvulnerability(context.Duration(invulnerabilityDuration));
            var radius = context.Area(blastRadius);
            CombatQueries.DamageInRadius(destination, radius, GetDamage(context), context.TargetMask);

            if (arrivalRing != null)
                PoolManager.Spawn(arrivalRing, destination, Quaternion.identity).Play(radius);

            return true;
        }
    }
}
