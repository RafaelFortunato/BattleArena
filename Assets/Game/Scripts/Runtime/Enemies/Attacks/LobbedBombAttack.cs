using BattleArena.Combat;
using BattleArena.Core;
using BattleArena.Pooling;
using UnityEngine;

namespace BattleArena.Enemies.Attacks
{
    /// <summary>Throws a bomb in an arc at the player's position; the landing zone is marked while it flies.</summary>
    public sealed class LobbedBombAttack : TelegraphedAttack
    {
        [SerializeField] private DelayedAreaStrike bombPrefab;
        [SerializeField] private Transform muzzle;
        [SerializeField, Min(0.1f)] private float flightTime = 1.1f;
        [SerializeField, Min(0.5f)] private float blastRadius = 2.3f;

        /// <summary>Lobs a bomb from the muzzle to where the player stands.</summary>
        protected override void Execute(in EnemyTickContext context)
        {
            var target = context.TargetPosition;
            target.y = transform.position.y;

            var bomb = PoolManager.Spawn(bombPrefab, target, Quaternion.identity);
            bomb.ArmLobbed(muzzle.position, blastRadius, flightTime, ScaledDamage(BaseDamage), GameLayers.PlayerMask);
        }
    }
}
