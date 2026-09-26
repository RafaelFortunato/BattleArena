namespace BattleArena.Combat
{
    public interface IDamageable
    {
        bool IsAlive { get; }

        void TakeDamage(float amount);
    }

    /// <summary>Anything whose movement speed can be temporarily reduced (e.g. by Frost Nova).</summary>
    public interface ISlowable
    {
        /// <param name="speedMultiplier">0..1, where 0.5 halves the speed.</param>
        void ApplySlow(float speedMultiplier, float duration);
    }
}
