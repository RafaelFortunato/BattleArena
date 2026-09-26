using UnityEngine;

namespace BattleArena.Enemies
{
    /// <summary>
    /// Melee hit when touching the player. Range, damage and rate come from the <see cref="EnemyDefinition"/>.
    /// Tests the path travelled since the last tick, so fast enemies can't pass through the player unharmed.
    /// </summary>
    public sealed class ContactAttack : EnemyAttack
    {
        private float _readyAt;
        private Vector3 _lastPosition;

        public override void OnSpawned()
        {
            _readyAt = 0f;
            _lastPosition = transform.position;
        }

        public override void Tick(in EnemyTickContext context)
        {
            var current = transform.position;
            var previous = _lastPosition;
            _lastPosition = current;

            if (context.Now < _readyAt)
                return;

            var definition = Owner.Definition;
            var range = definition.AttackRange;
            if (SqrDistanceToSegment(context.TargetPosition, previous, current) > range * range)
                return;

            ScaledDamage(definition.ContactDamage).ApplyTo(context.Target);
            _readyAt = context.Now + definition.AttackCooldown;
        }
    }
}
