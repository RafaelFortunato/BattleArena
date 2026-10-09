namespace BattleArena.Combat
{
    public interface IDamageable
    {
        bool IsAlive { get; }

        void TakeDamage(float amount, bool isCritical = false);
    }

    /// <summary>A hit received by a <see cref="Health"/>: full damage after armor, not clamped to the remaining health.</summary>
    public readonly struct DamageResult
    {
        public readonly float Amount;
        public readonly bool IsCritical;

        public DamageResult(float amount, bool isCritical)
        {
            Amount = amount;
            IsCritical = isCritical;
        }
    }

    /// <summary>Anything whose movement speed can be temporarily reduced (e.g. by Frost Nova).</summary>
    public interface ISlowable
    {
        /// <param name="speedMultiplier">0..1, where 0.5 halves the speed.</param>
        /// <param name="chill">A frost slow: the target also shows it (icy tint, frost particles) while it lasts.</param>
        void ApplySlow(float speedMultiplier, float duration, bool chill = false);
    }
}
