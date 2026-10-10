using System.Collections;
using BattleArena.Combat;
using BattleArena.Enemies.Movement;
using UnityEngine;

namespace BattleArena.Enemies
{
    /// <summary>
    /// Centralised enemy "brain". Instead of one Update per enemy, every live enemy is ticked
    /// here a few times per second, which keeps pathfinding and attack checks cheap on WebGL.
    /// Flying enemies, which bypass the NavMesh, are integrated here every frame in a single loop.
    /// </summary>
    public sealed class EnemyDirector : MonoBehaviour
    {
        [SerializeField] private Health player;
        [SerializeField, Range(0.05f, 1f)] private float tickInterval = 0.15f;

        /// <summary>Starts the tick loop.</summary>
        private void OnEnable() => StartCoroutine(TickLoop());

        /// <summary>Moves every flyer this frame in one pass.</summary>
        private void Update()
        {
            var flyers = FlyingMovement.Active;
            if (flyers.Count == 0 || !player.IsAlive)
                return;

            var target = player.transform.position;
            var deltaTime = Time.deltaTime;
            for (var i = flyers.Count - 1; i >= 0; i--)
                flyers[i].Integrate(target, deltaTime);
        }

        /// <summary>Ticks every live enemy a few times per second while the player is alive.</summary>
        private IEnumerator TickLoop()
        {
            var wait = new WaitForSeconds(tickInterval);
            var playerTransform = player.transform;
            var lastTick = Time.time;

            while (true)
            {
                var now = Time.time;
                if (player.IsAlive)
                {
                    var context = new EnemyTickContext(playerTransform.position, player, now, now - lastTick);
                    var enemies = EnemyRegistry.Active;

                    // Iterate backwards: a tick can remove its enemy (swap-remove moves an already-ticked
                    // enemy into this slot) or spawn new ones at the end (ticked next time).
                    for (var i = enemies.Count - 1; i >= 0; i--)
                    {
                        if (i < enemies.Count)
                            enemies[i].Tick(context);
                    }
                }
                lastTick = now;

                yield return wait;
            }
        }
    }
}
