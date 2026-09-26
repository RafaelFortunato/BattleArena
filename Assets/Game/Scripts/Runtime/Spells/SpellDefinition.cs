using UnityEngine;

namespace BattleArena.Spells
{
    /// <summary>Everything a spell needs to know about its caster at the moment it is cast.</summary>
    public readonly struct SpellContext
    {
        public readonly Transform Caster;
        public readonly Vector3 Origin;
        public readonly Vector3 AimDirection;
        public readonly float DamageMultiplier;
        public readonly int TargetMask;

        public SpellContext(Transform caster, Vector3 origin, Vector3 aimDirection, float damageMultiplier, int targetMask)
        {
            Caster = caster;
            Origin = origin;
            AimDirection = aimDirection;
            DamageMultiplier = damageMultiplier;
            TargetMask = targetMask;
        }
    }

    /// <summary>
    /// Strategy base for player-activated spells. Each concrete spell is a data asset with its own
    /// behaviour, so new spells can be added without touching <see cref="Player.SpellCaster"/>.
    /// </summary>
    public abstract class SpellDefinition : ScriptableObject
    {
        [SerializeField] private string displayName = "Spell";
        [SerializeField, TextArea] private string description;
        [SerializeField] private Color color = Color.white;
        [SerializeField, Min(1)] private int unlockLevel = 5;
        [SerializeField, Min(0f)] private float cooldown = 5f;
        [SerializeField, Min(0f)] private float baseDamage = 30f;

        public string DisplayName => displayName;
        public string Description => description;
        public Color Color => color;
        public int UnlockLevel => unlockLevel;
        public float Cooldown => cooldown;

        protected float GetDamage(in SpellContext context) => baseDamage * context.DamageMultiplier;

        /// <summary>Performs the spell. Returns false if it could not be cast, in which case no cooldown is applied.</summary>
        public abstract bool Cast(in SpellContext context);
    }
}
