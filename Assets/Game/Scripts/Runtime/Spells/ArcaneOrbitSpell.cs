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

        // Area grows the orbs (and widens the ring a little), Duration keeps them out longer, Projectiles adds orbs.
        public override SpellStats ScalesWith => SpellStats.Area | SpellStats.Duration | SpellStats.Projectiles;

        public override bool Cast(in SpellContext context)
        {
            var caster = context.Caster.Transform;
            var orbs = PoolManager.Spawn(orbsPrefab, caster.position, Quaternion.identity);
            // Area grows the orbs like any other area effect (hit size scales linearly) but widens the ring only by the
            // square root, so a bigger Area means bigger orbs close to you rather than small orbs drifting further away.
            var area = context.Modifiers.AreaMultiplier;
            orbs.Activate(caster, GetDamage(context), orbCount + context.Modifiers.ExtraProjectiles,
                radius * Mathf.Sqrt(area), area, context.Duration(duration), degreesPerSecond);
            return true;
        }
    }
}
