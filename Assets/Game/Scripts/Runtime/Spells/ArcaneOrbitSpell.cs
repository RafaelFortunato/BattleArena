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

        /// <summary>
        /// Area grows the orbs (and widens the ring a little), Duration keeps them out longer, Projectiles adds orbs.
        /// </summary>
        public override SpellStats ScalesWith => SpellStats.Area | SpellStats.Duration | SpellStats.Projectiles;

        /// <summary>
        /// Spawns the ring of orbs around the caster. Area grows the orbs like any other area effect but widens the
        /// ring only by the square root, so a bigger Area means bigger orbs close to you rather than small orbs
        /// drifting further away.
        /// </summary>
        public override bool Cast(in SpellContext context)
        {
            var caster = context.Caster.Transform;
            var orbs = PoolManager.Spawn(orbsPrefab, caster.position, Quaternion.identity);
            var area = context.Modifiers.AreaMultiplier;
            orbs.Activate(caster, GetDamage(context), orbCount + context.Modifiers.ExtraProjectiles,
                radius * Mathf.Sqrt(area), area, context.Duration(duration), degreesPerSecond);
            return true;
        }
    }
}
