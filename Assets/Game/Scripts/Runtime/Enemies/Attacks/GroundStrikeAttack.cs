using BattleArena.Combat;
using BattleArena.Core;
using BattleArena.Pooling;
using UnityEngine;

namespace BattleArena.Enemies.Attacks
{
    /// <summary>Curses the ground under (and around) the player: telegraphed circles that erupt after a delay.</summary>
    public sealed class GroundStrikeAttack : TelegraphedAttack
    {
        [SerializeField] private DelayedAreaStrike strikePrefab;
        [SerializeField, Min(0.5f)] private float strikeRadius = 3f;
        [SerializeField, Min(0.1f)] private float strikeDelay = 1.2f;
        [SerializeField, Min(1)] private int strikeCount = 1;
        [Tooltip("Extra strikes land at random points within this distance of the player.")]
        [SerializeField, Min(0f)] private float scatter = 3.5f;

        /// <summary>Arms a strike under the player and scatters any extra strikes around them.</summary>
        protected override void Execute(in EnemyTickContext context)
        {
            var damage = ScaledDamage(BaseDamage);
            for (var i = 0; i < strikeCount; i++)
            {
                var position = context.TargetPosition;
                if (i > 0)
                {
                    var offset = Random.insideUnitCircle * scatter;
                    position += new Vector3(offset.x, 0f, offset.y);
                }
                position.y = transform.position.y;

                var strike = PoolManager.Spawn(strikePrefab, position, Quaternion.identity);
                strike.Arm(strikeRadius, strikeDelay, damage, GameLayers.PlayerMask);
            }
        }
    }
}
