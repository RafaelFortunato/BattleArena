using System.Globalization;
using BattleArena.Combat;
using UnityEngine;
using UnityEngine.UIElements;

namespace BattleArena.Spells
{
    /// <summary>
    /// Shared stats that change how a spell behaves, shown on its card and in the pause menu. Attack, crit and
    /// cooldown reduction are left out: every damaging spell uses them.
    /// </summary>
    [System.Flags]
    public enum SpellStats
    {
        None = 0,
        Area = 1,
        Duration = 2,
        Projectiles = 4,
        Pierce = 8
    }

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
        /// <summary>The caster's flat Attack; a spell deals a percentage of it.</summary>
        public readonly float Attack;
        /// <summary>This spell's own damage upgrades (1 = none), applied on top of its share of Attack.</summary>
        public readonly float DamageMultiplier;
        public readonly float CritChance;
        public readonly float CritMultiplier;
        public readonly float AreaMultiplier;
        public readonly float DurationMultiplier;
        public readonly int ExtraProjectiles;
        public readonly int ExtraPierce;
        public readonly int ExtraBounces;

        public SpellModifiers(float attack, float critChance, float critMultiplier, float areaMultiplier,
            float durationMultiplier, int extraProjectiles, int extraPierce, int extraBounces = 0, float damageMultiplier = 1f)
        {
            Attack = attack;
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
            Attack,
            CritChance,
            CritMultiplier,
            AreaMultiplier * (1f + upgrades.Total(SpellUpgradeKind.Area)),
            DurationMultiplier * (1f + upgrades.Total(SpellUpgradeKind.Duration)),
            ExtraProjectiles + upgrades.TotalInt(SpellUpgradeKind.ExtraProjectiles),
            ExtraPierce,
            ExtraBounces + upgrades.TotalInt(SpellUpgradeKind.ExtraBounces),
            DamageMultiplier * (1f + upgrades.Total(SpellUpgradeKind.Damage)));
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

        /// <summary>Damage worth <paramref name="attackRatio"/> of the caster's Attack (1 = 100%), after this spell's upgrades.</summary>
        public DamageProfile Damage(float attackRatio) =>
            new(Modifiers.Attack * attackRatio * Modifiers.DamageMultiplier, Modifiers.CritChance, Modifiers.CritMultiplier);

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
        [Tooltip("Damage as a share of the caster's Attack: 3.5 deals 350% of Attack.")]
        [SerializeField, Min(0f)] private float attackRatio = 3f;
        [Tooltip("What one share of that damage is, for the card text: \"per meteor\", \"every 0.5s\". Empty for a single hit.")]
        [SerializeField] private string damageUnit;
        [Tooltip("Spell upgrade cards that can be offered for this spell.")]
        [SerializeField] private SpellUpgradeKind[] supportedUpgrades =
            { SpellUpgradeKind.CooldownReduction, SpellUpgradeKind.Damage };

        public string DisplayName => displayName;
        public string Description => description;
        public Color Color => color;
        public VectorImage Icon => icon;
        public float Cooldown => cooldown;
        public float AttackRatio => attackRatio;
        public string DamageUnit => damageUnit;

        /// <summary>
        /// What a spell that deals no damage shows on its card in place of "% ATK", e.g. ("Immunity", "2s").
        /// Damaging spells leave it empty.
        /// </summary>
        public virtual (string Label, string Value) CardHeadline => default;

        /// <summary>
        /// Which of the shared Area, Duration, Projectiles and Pierce stats change this spell. Declared by each spell
        /// beside the code that reads them, so the tags shown to the player cannot drift from what the spell does.
        /// </summary>
        public abstract SpellStats ScalesWith { get; }

        /// <summary>A share of Attack as players read it: 3.75 → "375%".</summary>
        public static string FormatAttackShare(float ratio) =>
            Mathf.RoundToInt(ratio * 100f).ToString(CultureInfo.InvariantCulture) + "%";

        public bool Supports(SpellUpgradeKind kind) => System.Array.IndexOf(supportedUpgrades, kind) >= 0;

        protected DamageProfile GetDamage(in SpellContext context) => context.Damage(attackRatio);

        /// <summary>Performs the spell. Returns false if it could not be cast, in which case no cooldown is applied.</summary>
        public abstract bool Cast(in SpellContext context);
    }
}
