using BattleArena.Combat;
using BattleArena.Pooling;
using UnityEngine;

namespace BattleArena.Spells
{
    /// <summary>Summons arcane orbs that circle the caster and damage every enemy they touch.</summary>
    [CreateAssetMenu(fileName = "ArcaneOrbit", menuName = "BattleArena/Spells/Arcane Orbit")]
    public sealed class ArcaneOrbitSpell : SpellDefinition
    {
        [SerializeField] private OrbitingOrbs orbsPrefab;
        [SerializeField, Min(1)] private int orbCount = 3;
        [SerializeField, Min(0.5f)] private float radius = 3.2f;
        [SerializeField, Min(0.1f)] private float duration = 6f;
        [SerializeField] private float degreesPerSecond = 220f;

        public override bool Cast(in SpellContext context)
        {
            var caster = context.Caster.Transform;
            var orbs = PoolManager.Spawn(orbsPrefab, caster.position, Quaternion.identity);
            orbs.Activate(caster, GetDamage(context), orbCount + context.Modifiers.ExtraProjectiles,
                context.Area(radius), context.Duration(duration), degreesPerSecond);
            return true;
        }
    }
}
