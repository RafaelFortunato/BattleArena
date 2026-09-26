using BattleArena.Combat;
using BattleArena.Pooling;
using UnityEngine;

namespace BattleArena.Spells
{
    /// <summary>Drops a lingering poison cloud on the nearest enemy that damages and slows over time.</summary>
    [CreateAssetMenu(fileName = "ToxicMiasma", menuName = "BattleArena/Spells/Toxic Miasma")]
    public sealed class ToxicMiasmaSpell : SpellDefinition
    {
        [SerializeField] private DamageZone zonePrefab;
        [SerializeField, Min(0.5f)] private float radius = 4.5f;
        [SerializeField, Min(0.1f)] private float duration = 5f;
        [SerializeField, Min(0.05f)] private float tickInterval = 0.5f;
        [SerializeField, Range(0.05f, 1f)] private float slowMultiplier = 0.7f;
        [SerializeField, Min(1f)] private float targetingRange = 15f;
        [SerializeField, Min(0f)] private float fallbackDistance = 5f;

        public override bool Cast(in SpellContext context)
        {
            var position = CombatQueries.TryFindNearest(context.Origin, targetingRange, context.TargetMask, out var target)
                ? target.position
                : context.CasterPosition + context.Caster.Facing * fallbackDistance;
            position.y = context.CasterPosition.y;

            var zone = PoolManager.Spawn(zonePrefab, position, Quaternion.identity);
            zone.Activate(context.Area(radius), context.Duration(duration), tickInterval, GetDamage(context), context.TargetMask, slowMultiplier);
            return true;
        }
    }
}
