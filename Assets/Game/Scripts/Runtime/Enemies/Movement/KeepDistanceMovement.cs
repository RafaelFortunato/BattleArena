using UnityEngine;
using UnityEngine.AI;

namespace BattleArena.Enemies
{
    /// <summary>Ranged kiting: approaches to a preferred distance, backs off when the player closes in.</summary>
    public sealed class KeepDistanceMovement : EnemyMovement
    {
        [SerializeField, Min(1f)] private float preferredDistance = 9f;
        [Tooltip("Dead zone around the preferred distance where the enemy holds position.")]
        [SerializeField, Min(0f)] private float tolerance = 2f;
        [SerializeField, Min(0.5f)] private float retreatStep = 4f;

        public override void Tick(in EnemyTickContext context)
        {
            var position = transform.position;
            var toTarget = Planar(context.TargetPosition - position);
            var distance = toTarget.magnitude;

            if (distance > preferredDistance + tolerance)
            {
                // Approach, but only up to the preferred ring.
                Owner.MoveTo(context.TargetPosition - toTarget / distance * preferredDistance);
            }
            else if (distance < preferredDistance - tolerance && distance > 0.01f)
            {
                var retreat = position - toTarget / distance * retreatStep;
                if (NavMesh.SamplePosition(retreat, out var hit, 2f, NavMesh.AllAreas))
                    Owner.MoveTo(hit.position);
            }
            else
            {
                Owner.Stop();
                Owner.Face(context.TargetPosition);
            }
        }
    }
}
