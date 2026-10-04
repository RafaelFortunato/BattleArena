using BattleArena.Pooling;
using BattleArena.Vfx;
using UnityEngine;
using UnityEngine.AI;

namespace BattleArena.Spells
{
    /// <summary>An escape: blinks forward and makes the caster immune to damage for a moment. Deals no damage.</summary>
    [CreateAssetMenu(fileName = "PhaseStep", menuName = "BattleArena/Spells/Phase Step")]
    public sealed class PhaseStepSpell : SpellDefinition
    {
        [SerializeField, Min(1f)] private float distance = 11f;
        [Tooltip("Seconds of damage immunity after the blink; scales with the Duration stat.")]
        [SerializeField, Min(0f)] private float invulnerabilityDuration = 2f;
        [SerializeField] private TimedRelease departEffect;
        [Tooltip("Arrival flourish, drawn at a fixed small size so it does not read as a damage area.")]
        [SerializeField] private AreaEffect arrivalRing;
        [SerializeField, Min(0.1f)] private float arrivalRingRadius = 1.5f;

        public override (string Label, string Value) CardHeadline =>
            ("Immunity", invulnerabilityDuration.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) + "s");

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

            if (arrivalRing != null)
                PoolManager.Spawn(arrivalRing, destination, Quaternion.identity).Play(arrivalRingRadius);

            return true;
        }
    }
}
