using BattleArena.Combat;
using BattleArena.Core;
using BattleArena.Pooling;
using UnityEngine;

namespace BattleArena.Enemies
{
    /// <summary>
    /// Kamikaze: when close, stops and lights a fuse (the blast zone is marked on the ground), then
    /// explodes. Killing it during the fuse does not defuse the blast.
    /// </summary>
    public sealed class SelfDestructAttack : TelegraphedAttack
    {
        [SerializeField] private DelayedAreaStrike blastPrefab;
        [SerializeField, Min(0.5f)] private float blastRadius = 3f;

        protected override void OnWindupStarted(in EnemyTickContext context)
        {
            var blast = PoolManager.Spawn(blastPrefab, transform.position, Quaternion.identity);
            blast.Arm(blastRadius, Windup, ScaledDamage(baseDamage), GameLayers.PlayerMask);
        }

        protected override void Execute(in EnemyTickContext context) => Owner.Vanish();
    }
}
