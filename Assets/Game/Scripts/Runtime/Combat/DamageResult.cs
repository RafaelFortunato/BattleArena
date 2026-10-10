namespace BattleArena.Combat
{
    /// <summary>A hit received by a <see cref="Health"/>: full damage after armor, not clamped to the remaining health.</summary>
    public readonly struct DamageResult
    {
        /// <summary>Damage dealt after armor.</summary>
        public readonly float Amount;

        /// <summary>True when the hit was a critical.</summary>
        public readonly bool IsCritical;

        /// <summary>Creates a hit record.</summary>
        public DamageResult(float amount, bool isCritical)
        {
            Amount = amount;
            IsCritical = isCritical;
        }
    }
}
