using UnityEngine;
using UnityEngine.AI;

namespace BattleArena.Enemies.Movement
{
    /// <summary>
    /// Runs away while the player is close and wanders otherwise (e.g. the Treasure Bunny). When a wall
    /// blocks the straight escape, it tries sideways directions so it slides along instead of getting stuck.
    /// </summary>
    public sealed class FleeMovement : EnemyMovement
    {
        private static readonly float[] EscapeAngles = { 0f, 45f, -45f, 90f, -90f, 135f, -135f };

        [SerializeField, Min(1f)] private float fleeDistance = 14f;
        [SerializeField, Min(1f)] private float fleeStep = 6f;
        [SerializeField, Min(1f)] private float wanderRadius = 6f;

        /// <summary>
        /// Flees from a nearby player along the first open escape direction; wanders when nobody is close.
        /// </summary>
        public override void Tick(in EnemyTickContext context)
        {
            var position = transform.position;
            var away = Planar(position - context.TargetPosition);
            var distance = away.magnitude;

            if (distance < fleeDistance && distance > 0.01f)
            {
                var direction = away / distance;
                foreach (var angle in EscapeAngles)
                {
                    var candidate = position + Quaternion.Euler(0f, angle, 0f) * direction * fleeStep;
                    // Accept the first direction with walkable ground that leads straight there.
                    if (NavMesh.SamplePosition(candidate, out var hit, 1.5f, NavMesh.AllAreas) &&
                        !NavMesh.Raycast(position, hit.position, out _, NavMesh.AllAreas))
                    {
                        Owner.MoveTo(hit.position);
                        return;
                    }
                }
                return;
            }

            // Wander when nobody is chasing.
            var agent = Owner.Agent;
            if (!agent.hasPath || agent.remainingDistance < 1f)
            {
                var offset = Random.insideUnitCircle * wanderRadius;
                if (NavMesh.SamplePosition(position + new Vector3(offset.x, 0f, offset.y), out var hit, 2f, NavMesh.AllAreas))
                    Owner.MoveTo(hit.position);
            }
        }
    }
}
