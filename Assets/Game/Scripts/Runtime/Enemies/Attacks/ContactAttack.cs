using UnityEngine;

namespace BattleArena.Enemies.Attacks
{
    /// <summary>
    /// Melee hit when touching the player. Range, damage and rate come from the <see cref="EnemyDefinition"/>.
    /// Tests the path travelled since the last tick, so fast enemies can't pass through the player unharmed.
    /// </summary>
    public sealed class ContactAttack : EnemyAttack
    {
        private float _readyAt;
        private Vector3 _lastPosition;

        /// <summary>Makes the attack ready and starts tracking movement from the spawn point.</summary>
        public override void OnSpawned()
        {
            _readyAt = 0f;
            _lastPosition = transform.position;
        }

        /// <summary>
        /// Hits the player when the path travelled since the last tick came within reach, then waits out the cooldown.
        /// </summary>
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
            Owner.NotifyAttack(0f);
            _readyAt = context.Now + definition.AttackCooldown;
        }
    }
}
