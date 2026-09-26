using BattleArena.Combat;
using BattleArena.Pooling;
using BattleArena.Vfx;
using UnityEngine;
using UnityEngine.AI;

namespace BattleArena.Enemies
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

        protected override float ActiveDuration => dashDistance / dashSpeed;

        protected override void Awake()
        {
            base.Awake();
            _defaultAcceleration = Owner.Agent.acceleration;
        }

        protected override bool CanStart(in EnemyTickContext context) =>
            base.CanStart(context) && PlanarSqrDistanceTo(context.TargetPosition) >= minRange * minRange;

        protected override void OnWindupStarted(in EnemyTickContext context)
        {
            // The direction is locked here, so the player can sidestep once the line appears.
            _direction = CombatQueries.Planar(context.TargetPosition - transform.position);
            if (telegraphPrefab != null)
                PoolManager.Spawn(telegraphPrefab, transform.position, Quaternion.identity)
                    .Show(transform.position, _direction, dashDistance, hitRadius * 2f, Windup);
        }

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

        protected override void TickActive(in EnemyTickContext context)
        {
            var current = transform.position;
            // Test the whole segment travelled since the last tick: at dash speed it can be several meters.
            if (!_hasHit && SqrDistanceToSegment(context.TargetPosition, _lastPosition, current) <= hitRadius * hitRadius)
            {
                _hasHit = true;
                ScaledDamage(baseDamage).ApplyTo(context.Target);
            }
            _lastPosition = current;
        }

        protected override void EndActive()
        {
            Owner.Agent.acceleration = _defaultAcceleration;
            Owner.ClearSpeedOverride();
            Owner.Stop();
        }
    }
}
