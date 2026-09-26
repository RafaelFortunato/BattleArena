using UnityEngine;

namespace BattleArena.Enemies
{
    /// <summary>
    /// Circles the player at a fixed radius, in a random direction per spawn. Each tick it aims a
    /// little further along the circle; the agent's speed decides how fast it actually orbits.
    /// </summary>
    public sealed class OrbitMovement : EnemyMovement
    {
        [SerializeField, Min(1f)] private float radius = 7.5f;
        [Tooltip("How far ahead on the circle to aim, in degrees. Larger values give smoother, wider paths.")]
        [SerializeField, Range(5f, 90f)] private float leadAngle = 35f;

        private float _direction = 1f;

        public override void OnSpawned() => _direction = Random.value < 0.5f ? -1f : 1f;

        public override void Tick(in EnemyTickContext context)
        {
            var fromTarget = Planar(transform.position - context.TargetPosition);
            var angle = Mathf.Atan2(fromTarget.z, fromTarget.x) + _direction * leadAngle * Mathf.Deg2Rad;
            Owner.MoveTo(context.TargetPosition + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * radius);
        }
    }
}
