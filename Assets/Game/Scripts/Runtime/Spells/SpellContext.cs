using BattleArena.Combat;
using UnityEngine;

namespace BattleArena.Spells
{
    /// <summary>Everything a spell needs to know about its caster at the moment it is cast.</summary>
    public readonly struct SpellContext
    {
        /// <summary>Who cast the spell.</summary>
        public readonly ISpellCaster Caster;

        /// <summary>Where the spell comes from (the staff's crystal).</summary>
        public readonly Vector3 Origin;

        /// <summary>Layers the spell can hit.</summary>
        public readonly int TargetMask;

        /// <summary>The caster's stats and this slot's upgrades at cast time.</summary>
        public readonly SpellModifiers Modifiers;

        /// <summary>Creates the context for one cast.</summary>
        public SpellContext(ISpellCaster caster, Vector3 origin, int targetMask, in SpellModifiers modifiers)
        {
            Caster = caster;
            Origin = origin;
            TargetMask = targetMask;
            Modifiers = modifiers;
        }

        /// <summary>The caster's position (at its feet).</summary>
        public Vector3 CasterPosition => Caster.Transform.position;

        /// <summary>Damage worth <paramref name="attackRatio"/> of the caster's Attack (1 = 100%), after this spell's upgrades.</summary>
        public DamageProfile Damage(float attackRatio) =>
            new(Modifiers.Attack * attackRatio * Modifiers.DamageMultiplier, Modifiers.CritChance, Modifiers.CritMultiplier);

        /// <summary><paramref name="radius"/> scaled by the Area stat.</summary>
        public float Area(float radius) => radius * Modifiers.AreaMultiplier;

        /// <summary><paramref name="seconds"/> scaled by the Duration stat.</summary>
        public float Duration(float seconds) => seconds * Modifiers.DurationMultiplier;
    }
}
