namespace BattleArena.Combat
{
    /// <summary>Anything whose movement speed can be temporarily reduced (e.g. by Frost Nova).</summary>
    public interface ISlowable
    {
        /// <summary>Slows the target for a while. The strongest active slow wins; a new one refreshes the duration.</summary>
        /// <param name="speedMultiplier">0..1, where 0.5 halves the speed.</param>
        /// <param name="duration">Seconds the slow lasts.</param>
        /// <param name="chill">A frost slow: the target also shows it (an icy rim) while it lasts.</param>
        void ApplySlow(float speedMultiplier, float duration, bool chill = false);
    }
}
