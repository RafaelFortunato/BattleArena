using BattleArena.Combat;
using UnityEngine;
using UnityEngine.UIElements;

namespace BattleArena.Spells
{
    /// <summary>What a spell may ask of whoever casts it. Keeps spells independent of player classes.</summary>
    public interface ISpellCaster
    {
        Transform Transform { get; }
        Health Health { get; }

        /// <summary>Planar direction the caster is facing.</summary>
        Vector3 Facing { get; }

        void Teleport(Vector3 position);
    }

    /// <summary>Caster stats that scale spells (snapshot taken at cast time).</summary>
    public readonly struct SpellModifiers
    {
        public readonly float DamageMultiplier;
        public readonly float CritChance;
        public readonly float CritMultiplier;
        public readonly float AreaMultiplier;
        public readonly float DurationMultiplier;
        public readonly int ExtraProjectiles;
        public readonly int ExtraPierce;
        public readonly int ExtraBounces;

        public SpellModifiers(float damageMultiplier, float critChance, float critMultiplier, float areaMultiplier,
            float durationMultiplier, int extraProjectiles, int extraPierce, int extraBounces = 0)
        {
            DamageMultiplier = damageMultiplier;
            CritChance = critChance;
            CritMultiplier = critMultiplier;
            AreaMultiplier = areaMultiplier;
            DurationMultiplier = durationMultiplier;
            ExtraProjectiles = extraProjectiles;
            ExtraPierce = extraPierce;
            ExtraBounces = extraBounces;
        }

        /// <summary>Layers one spell slot's upgrades on top of the caster's stats.</summary>
        public SpellModifiers With(SpellUpgradeState upgrades) => new(
            DamageMultiplier * (1f + upgrades.Total(SpellUpgradeKind.Damage)),
            CritChance,
            CritMultiplier,
            AreaMultiplier * (1f + upgrades.Total(SpellUpgradeKind.Area)),
            DurationMultiplier * (1f + upgrades.Total(SpellUpgradeKind.Duration)),
            ExtraProjectiles + upgrades.TotalInt(SpellUpgradeKind.ExtraProjectiles),
            ExtraPierce,
            ExtraBounces + upgrades.TotalInt(SpellUpgradeKind.ExtraBounces));
    }

    /// <summary>Everything a spell needs to know about its caster at the moment it is cast.</summary>
    public readonly struct SpellContext
    {
        public readonly ISpellCaster Caster;
        public readonly Vector3 Origin;
        public readonly int TargetMask;
        public readonly SpellModifiers Modifiers;

        public SpellContext(ISpellCaster caster, Vector3 origin, int targetMask, in SpellModifiers modifiers)
        {
            Caster = caster;
            Origin = origin;
            TargetMask = targetMask;
            Modifiers = modifiers;
        }

        public Vector3 CasterPosition => Caster.Transform.position;

        /// <summary>Scales a spell's base damage by the caster's stats.</summary>
        public DamageProfile Damage(float baseDamage) =>
            new(baseDamage * Modifiers.DamageMultiplier, Modifiers.CritChance, Modifiers.CritMultiplier);

        public float Area(float radius) => radius * Modifiers.AreaMultiplier;

        public float Duration(float seconds) => seconds * Modifiers.DurationMultiplier;
    }

    /// <summary>
    /// Strategy base for player-activated spells. Each concrete spell is a data asset with its own
    /// behaviour, so new spells can be added without touching the caster or the level-up system.
    /// </summary>
    public abstract class SpellDefinition : ScriptableObject
    {
        [SerializeField] private string displayName = "Spell";
        [SerializeField, TextArea] private string description;
        [SerializeField] private Color color = Color.white;
        [Tooltip("White line icon, tinted with the spell colour by the UI.")]
        [SerializeField] private VectorImage icon;
        [SerializeField, Min(0f)] private float cooldown = 5f;
        [SerializeField, Min(0f)] private float baseDamage = 30f;
        [Tooltip("Spell upgrade cards that can be offered for this spell.")]
        [SerializeField] private SpellUpgradeKind[] supportedUpgrades =
            { SpellUpgradeKind.CooldownReduction, SpellUpgradeKind.Damage };

        public string DisplayName => displayName;
        public string Description => description;
        public Color Color => color;
        public VectorImage Icon => icon;
        public float Cooldown => cooldown;

        public bool Supports(SpellUpgradeKind kind) => System.Array.IndexOf(supportedUpgrades, kind) >= 0;

        protected DamageProfile GetDamage(in SpellContext context) => context.Damage(baseDamage);

        /// <summary>Performs the spell. Returns false if it could not be cast, in which case no cooldown is applied.</summary>
        public abstract bool Cast(in SpellContext context);
    }
}
