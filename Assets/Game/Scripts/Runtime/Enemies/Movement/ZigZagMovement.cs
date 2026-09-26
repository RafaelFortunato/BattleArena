using UnityEngine;

namespace BattleArena.Enemies
{
    /// <summary>Weaves side to side while closing in, making the enemy harder to hit.</summary>
    public sealed class ZigZagMovement : EnemyMovement
    {
        [SerializeField, Min(0f)] private float amplitude = 2.5f;
        [SerializeField, Min(0.05f)] private float frequency = 1.1f;
        [SerializeField, Min(0.5f)] private float lookAhead = 4f;
        [Tooltip("Within this distance the enemy stops weaving and goes straight in.")]
        [SerializeField, Min(0f)] private float directRange = 2.5f;

        private float _phase;

        public override void OnSpawned() => _phase = Random.value * Mathf.PI * 2f;

        public override void Tick(in EnemyTickContext context)
        {
            var position = transform.position;
            var toTarget = Planar(context.TargetPosition - position);
            var distance = toTarget.magnitude;
            if (distance <= directRange)
            {
                Owner.MoveTo(context.TargetPosition);
                return;
            }

            var forward = toTarget / distance;
            var side = new Vector3(forward.z, 0f, -forward.x);
            var sway = Mathf.Sin(context.Now * frequency * Mathf.PI * 2f + _phase) * amplitude;
            Owner.MoveTo(position + forward * Mathf.Min(lookAhead, distance) + side * sway);
        }
    }
}
