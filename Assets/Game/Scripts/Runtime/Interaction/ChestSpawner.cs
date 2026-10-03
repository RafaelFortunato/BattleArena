using System.Collections;
using BattleArena.Audio;
using BattleArena.Core;
using BattleArena.Pooling;
using UnityEngine;
using UnityEngine.AI;

namespace BattleArena.Interaction
{
    /// <summary>Periodically places treasure chests on free ground near the player during a match.</summary>
    public sealed class ChestSpawner : MonoBehaviour
    {
        private const int MaxPlacementAttempts = 12;

        [SerializeField] private TreasureChest chestPrefab;
        [SerializeField] private Transform player;
        [SerializeField, Min(0f)] private float firstSpawnDelay = 25f;
        [SerializeField, Min(1f)] private float spawnInterval = 40f;
        [SerializeField, Min(1)] private int maxActiveChests = 2;
        [SerializeField, Min(1f)] private float minDistance = 7f;
        [SerializeField, Min(1f)] private float maxDistance = 13f;
        [Tooltip("Chests are not placed where they would overlap these colliders (rocks, trees, walls).")]
        [SerializeField] private LayerMask blockingLayers = 1;
        [SerializeField, Min(0.1f)] private float clearanceRadius = 1.2f;
        [SerializeField] private TimedRelease spawnEffect;
        [SerializeField] private SoundEvent spawnSound;

        private Coroutine _spawnLoop;
        private int _activeChests;

        private void OnEnable() => EventBus<GameStateChangedEvent>.Subscribe(OnGameStateChanged);

        private void OnDisable()
        {
            EventBus<GameStateChangedEvent>.Unsubscribe(OnGameStateChanged);
            _spawnLoop = null;
        }

        private void OnGameStateChanged(GameStateChangedEvent evt)
        {
            if (evt.Current == GameState.Playing)
            {
                _spawnLoop ??= StartCoroutine(SpawnLoop());
            }
            else if (evt.Current is GameState.Victory or GameState.Defeat && _spawnLoop != null)
            {
                StopCoroutine(_spawnLoop);
                _spawnLoop = null;
            }
        }

        private IEnumerator SpawnLoop()
        {
            yield return WaitCache.Get(firstSpawnDelay);
            while (true)
            {
                if (_activeChests < maxActiveChests && TryFindSpot(out var position))
                    Spawn(position);
                yield return WaitCache.Get(spawnInterval);
            }
        }

        private void Spawn(Vector3 position)
        {
            var chest = PoolManager.Spawn(chestPrefab, position, Quaternion.Euler(0f, Random.Range(0f, 360f), 0f));
            ((IInteractable)chest).Removed += OnChestRemoved;
            _activeChests++;

            if (spawnEffect != null)
                PoolManager.Spawn(spawnEffect, position + Vector3.up * 0.5f, Quaternion.identity);
            AudioManager.PlayAt(spawnSound, position);
            EventBus<NotificationEvent>.Raise(new NotificationEvent("A treasure chest appeared nearby!"));
        }

        private void OnChestRemoved(IInteractable chest)
        {
            chest.Removed -= OnChestRemoved;
            _activeChests--;
        }

        private bool TryFindSpot(out Vector3 position)
        {
            var center = player.position;
            for (var attempt = 0; attempt < MaxPlacementAttempts; attempt++)
            {
                var direction = Random.insideUnitCircle.normalized;
                var candidate = center + new Vector3(direction.x, 0f, direction.y) * Random.Range(minDistance, maxDistance);

                if (!NavMesh.SamplePosition(candidate, out var hit, 1f, NavMesh.AllAreas))
                    continue;
                if (Physics.CheckSphere(hit.position + Vector3.up * (clearanceRadius + 0.1f), clearanceRadius, blockingLayers, QueryTriggerInteraction.Ignore))
                    continue;

                position = hit.position;
                return true;
            }

            position = default;
            return false;
        }
    }
}
