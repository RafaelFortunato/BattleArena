namespace BattleArena.Combat
{
    /// <summary>Anything attacks can hurt: the player, enemies and the boss (through <see cref="Health"/>).</summary>
    public interface IDamageable
    {
        /// <summary>False once the target has died; attacks skip dead targets.</summary>
        bool IsAlive { get; }

        /// <summary>Applies one hit of <paramref name="amount"/> damage, already rolled for critical.</summary>
        /// <param name="amount">Damage before the target's own reductions (armor, invulnerability).</param>
        /// <param name="isCritical">True for a critical hit, so feedback can stand out.</param>
        void TakeDamage(float amount, bool isCritical = false);
    }
}
