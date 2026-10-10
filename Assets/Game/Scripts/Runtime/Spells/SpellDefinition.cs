using System.Globalization;
using BattleArena.Combat;
using UnityEngine;
using UnityEngine.UIElements;

namespace BattleArena.Spells
{
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

        /// <summary>The spell's name.</summary>
        public string DisplayName => displayName;

        /// <summary>What the spell does, for its card.</summary>
        public string Description => description;

        /// <summary>The spell's colour, used by its card and HUD slot.</summary>
        public Color Color => color;

        /// <summary>The spell's line icon.</summary>
        public VectorImage Icon => icon;

        /// <summary>Base seconds between casts.</summary>
        public float Cooldown => cooldown;

        /// <summary>Damage as a share of the caster's Attack (3.5 = 350%).</summary>
        public float AttackRatio => attackRatio;

        /// <summary>What one share of the damage is, for the card text, e.g. "per meteor"; empty for a single hit.</summary>
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

        /// <summary>
        /// How much more a spell upgrade card gives this spell than its base amount (1 = as printed). Lets a spell whose
        /// single unit is small, like one meteor, get a bigger step from the same card.
        /// </summary>
        public virtual float UpgradeScale(SpellUpgradeKind kind) => 1f;

        /// <summary>A share of Attack as players read it: 3.75 → "375%".</summary>
        public static string FormatAttackShare(float ratio) =>
            Mathf.RoundToInt(ratio * 100f).ToString(CultureInfo.InvariantCulture) + "%";

        /// <summary>True when upgrade cards of <paramref name="kind"/> can be offered for this spell.</summary>
        public bool Supports(SpellUpgradeKind kind) => System.Array.IndexOf(supportedUpgrades, kind) >= 0;

        /// <summary>Performs the spell. Returns false if it could not be cast, in which case no cooldown is applied.</summary>
        public abstract bool Cast(in SpellContext context);

        /// <summary>The spell's damage for this cast: its share of Attack after its upgrades, with the caster's crit.</summary>
        protected DamageProfile GetDamage(in SpellContext context) => context.Damage(attackRatio);
    }
}
