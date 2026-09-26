using System.Collections;
using BattleArena.Combat;
using UnityEngine;

namespace BattleArena.Enemies
{
    /// <summary>
    /// Centralised enemy "brain". Instead of one Update per enemy, every live enemy is ticked
    /// here a few times per second, which keeps pathfinding and attack checks cheap on WebGL.
    /// </summary>
    public sealed class EnemyDirector : MonoBehaviour
    {
        [SerializeField] private Health player;
        [SerializeField, Range(0.05f, 1f)] private float tickInterval = 0.15f;

        private void OnEnable() => StartCoroutine(TickLoop());

        private IEnumerator TickLoop()
        {
            var wait = new WaitForSeconds(tickInterval);
            var playerTransform = player.transform;

            while (true)
            {
                if (player.IsAlive)
                {
                    var targetPosition = playerTransform.position;
                    var now = Time.time;
                    var enemies = EnemyRegistry.Active;

                    // Iterate backwards: a tick can kill the player and trigger cleanup.
                    for (var i = enemies.Count - 1; i >= 0; i--)
                    {
                        if (i < enemies.Count)
                            enemies[i].Tick(targetPosition, player, now);
                    }
                }

                yield return wait;
            }
        }
    }
}
