namespace BattleArena.Enemies
{
    /// <summary>Walks straight at the player.</summary>
    public sealed class ChaseMovement : EnemyMovement
    {
        public override void Tick(in EnemyTickContext context) => Owner.MoveTo(context.TargetPosition);
    }
}
