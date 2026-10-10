using BattleArena.Combat;
using BattleArena.Pooling;
using BattleArena.Vfx;
using UnityEngine;
using UnityEngine.AI;

namespace BattleArena.Enemies.Attacks
{
    /// <summary>
    /// Charger attack: locks a direction and shows a line on the ground during the wind-up, then
    /// dashes along it at high speed, hitting the player once if the path crosses them.
    /// </summary>
    public sealed class DashAttack : TelegraphedAttack
    {
        [SerializeField] private LineTelegraph telegraphPrefab;
        [SerializeField, Min(0f)] private float minRange = 3f;
        [SerializeField, Min(1f)] private float dashDistance = 11f;
        [SerializeField, Min(1f)] private float dashSpeed = 20f;
        [SerializeField, Min(0.1f)] private float hitRadius = 1.4f;
        [SerializeField, Min(1f)] private float dashAcceleration = 200f;

        private Vector3 _direction;
        private Vector3 _lastPosition;
        private float _defaultAcceleration;
        private bool _hasHit;

        /// <summary>How long the dash takes at dash speed.</summary>
        protected override float ActiveDuration => dashDistance / dashSpeed;

        /// <summary>Remembers the agent's normal acceleration.</summary>
        protected override void Awake()
        {
            base.Awake();
            _defaultAcceleration = Owner.Agent.acceleration;
        }

        /// <summary>Only dashes from far enough away for the dash to make sense.</summary>
        protected override bool CanStart(in EnemyTickContext context) =>
            base.CanStart(context) && PlanarSqrDistanceTo(context.TargetPosition) >= minRange * minRange;

        /// <summary>
        /// Locks the dash direction and shows the line on the ground, so the player can sidestep once it appears.
        /// </summary>
        protected override void OnWindupStarted(in EnemyTickContext context)
        {
            _direction = CombatQueries.Planar(context.TargetPosition - transform.position);
            if (telegraphPrefab != null)
                PoolManager.Spawn(telegraphPrefab, transform.position, Quaternion.identity)
                    .Show(transform.position, _direction, dashDistance, hitRadius * 2f, Windup);
        }

        /// <summary>
        /// Dashes along the locked direction, stopping short at walls instead of pathing around them.
        /// </summary>
        protected override void Execute(in EnemyTickContext context)
        {
            var start = transform.position;
            var end = start + _direction * dashDistance;
            // Stop at walls and obstacles instead of trying to path around them.
            if (NavMesh.Raycast(start, end, out var hit, NavMesh.AllAreas))
                end = hit.position;

            _hasHit = false;
            _lastPosition = start;

            var agent = Owner.Agent;
            agent.acceleration = dashAcceleration;
            Owner.OverrideSpeed(dashSpeed);
            Owner.Hold(stopMoving: false);
            Owner.MoveTo(end);
        }

        /// <summary>
        /// Hits the player once if the segment travelled since the last tick passed close enough (at dash speed it can
        /// be several metres).
        /// </summary>
        protected override void TickActive(in EnemyTickContext context)
        {
            var current = transform.position;
            if (!_hasHit && SqrDistanceToSegment(context.TargetPosition, _lastPosition, current) <= hitRadius * hitRadius)
            {
                _hasHit = true;
                ScaledDamage(BaseDamage).ApplyTo(context.Target);
            }
            _lastPosition = current;
        }

        /// <summary>Restores normal acceleration and speed and stops.</summary>
        protected override void EndActive()
        {
            Owner.Agent.acceleration = _defaultAcceleration;
            Owner.ClearSpeedOverride();
            Owner.Stop();
        }
    }
}
