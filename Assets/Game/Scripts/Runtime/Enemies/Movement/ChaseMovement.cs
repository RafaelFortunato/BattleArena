namespace BattleArena.Enemies.Movement
{
    /// <summary>Walks straight at the player.</summary>
    public sealed class ChaseMovement : EnemyMovement
    {
        /// <summary>Paths straight to the player.</summary>
        public override void Tick(in EnemyTickContext context) => Owner.MoveTo(context.TargetPosition);
    }
}
