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
        [SerializeField] private AreaEffect ringEffect;

        /// <summary>Area is the burst size, Duration how long the slow lasts.</summary>
        public override SpellStats ScalesWith => SpellStats.Area | SpellStats.Duration;

        /// <summary>Damages and chills every enemy in the burst and plays the ring effect.</summary>
        public override bool Cast(in SpellContext context)
        {
            var center = context.CasterPosition;
            var area = context.Area(radius);
            CombatQueries.DamageAndSlowInRadius(center, area, GetDamage(context), context.TargetMask, slowMultiplier,
                context.Duration(slowDuration), chill: true);

            if (ringEffect != null)
                PoolManager.Spawn(ringEffect, center, Quaternion.identity).Play(area);

            return true;
        }
    }
}
