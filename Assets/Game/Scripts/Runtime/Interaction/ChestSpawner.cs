using System.Collections;
using BattleArena.Audio;
using BattleArena.Core;
using BattleArena.Pooling;
using UnityEngine;
using UnityEngine.AI;

namespace BattleArena.Interaction
{
    /// <summary>
    /// Periodically places treasure chests on free, reachable ground anywhere in the arena, away from the player and
    /// never on screen, so finding them takes exploring.
    /// </summary>
    public sealed class ChestSpawner : MonoBehaviour
    {
        private const int MaxPlacementAttempts = 30;

        [SerializeField] private TreasureChest chestPrefab;
        [SerializeField] private Transform player;
        [SerializeField, Min(0f)] private float firstSpawnDelay = 25f;
        [SerializeField, Min(1f)] private float spawnInterval = 40f;
        [SerializeField, Min(1)] private int maxActiveChests = 2;
        [Tooltip("Chests are placed within this distance of the arena centre on both axes.")]
        [SerializeField, Min(1f)] private float arenaHalfExtent = 36f;
        [SerializeField, Min(1f)] private float minDistance = 16f;
        [Tooltip("How far outside the screen edges (in screen fractions) a chest must be placed.")]
        [SerializeField, Range(0f, 0.5f)] private float offscreenMargin = 0.08f;
        [Tooltip("Seconds before trying again when no free off-screen spot was found (e.g. the player is in a corner).")]
        [SerializeField, Min(0.5f)] private float retryDelay = 3f;
        [Tooltip("Chests are not placed where they would overlap these colliders (rocks, trees, walls).")]
        [SerializeField] private LayerMask blockingLayers = 1;
        [SerializeField, Min(0.1f)] private float clearanceRadius = 1.2f;
        [SerializeField] private TimedRelease spawnEffect;
        [SerializeField] private SoundEvent spawnSound;

        private NavMeshPath _path;
        private Coroutine _spawnLoop;
        private int _activeChests;
        private Camera _camera;

        /// <summary>Listens for match state changes.</summary>
        private void OnEnable() => EventBus<GameStateChangedEvent>.Subscribe(OnGameStateChanged);

        /// <summary>Stops listening; the spawn loop dies with the component.</summary>
        private void OnDisable()
        {
            EventBus<GameStateChangedEvent>.Unsubscribe(OnGameStateChanged);
            _spawnLoop = null;
        }

        /// <summary>
        /// Starts spawning when play begins and stops when the match ends. Pauses leave the loop running (time is
        /// frozen anyway).
        /// </summary>
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

        /// <summary>
        /// After a first delay, places a chest on every interval while fewer than the maximum are out, retrying sooner
        /// when no spot was free.
        /// </summary>
        private IEnumerator SpawnLoop()
        {
            yield return WaitCache.Get(firstSpawnDelay);
            while (true)
            {
                if (_activeChests >= maxActiveChests)
                {
                    yield return WaitCache.Get(spawnInterval);
                    continue;
                }

                if (TryFindSpot(out var position))
                {
                    Spawn(position);
                    yield return WaitCache.Get(spawnInterval);
                }
                else
                {
                    yield return WaitCache.Get(retryDelay);
                }
            }
        }


        /// <summary>Places a chest with its spawn effect and sound, and tells the player one appeared.</summary>
        private void Spawn(Vector3 position)
        {
            var chest = PoolManager.Spawn(chestPrefab, position, FacingCamera());
            ((IInteractable)chest).Removed += OnChestRemoved;
            _activeChests++;

            if (spawnEffect != null)
                PoolManager.Spawn(spawnEffect, position + Vector3.up * 0.5f, Quaternion.identity);
            AudioManager.PlayAt(spawnSound, position);
            EventBus<NotificationEvent>.Raise(new NotificationEvent("A treasure chest appeared somewhere!"));
        }

        /// <summary>
        /// Rotation that shows the chest's front to the camera: the model's lock is on its -Z side, so +Z points away
        /// from the camera.
        /// </summary>
        private Quaternion FacingCamera()
        {
            if (_camera == null)
                _camera = Camera.main;
            if (_camera == null)
                return Quaternion.identity;

            var away = Vector3.ProjectOnPlane(_camera.transform.forward, Vector3.up);
            return away.sqrMagnitude > 0.0001f ? Quaternion.LookRotation(away) : Quaternion.identity;
        }

        /// <summary>Frees a chest slot once a chest is opened and gone.</summary>
        private void OnChestRemoved(IInteractable chest)
        {
            chest.Removed -= OnChestRemoved;
            _activeChests--;
        }

        /// <summary>
        /// Samples random arena points until one is on the NavMesh, far enough from the player, clear of obstacles, off
        /// screen and reachable.
        /// </summary>
        private bool TryFindSpot(out Vector3 position)
        {
            var origin = player.position;
            for (var attempt = 0; attempt < MaxPlacementAttempts; attempt++)
            {
                var candidate = new Vector3(Random.Range(-arenaHalfExtent, arenaHalfExtent), 0f, Random.Range(-arenaHalfExtent, arenaHalfExtent));
                if (!NavMesh.SamplePosition(candidate, out var hit, 1f, NavMesh.AllAreas))
                    continue;

                var offset = hit.position - origin;
                offset.y = 0f;
                if (offset.sqrMagnitude < minDistance * minDistance)
                    continue;
                if (Physics.CheckSphere(hit.position + Vector3.up * (clearanceRadius + 0.1f), clearanceRadius, blockingLayers, QueryTriggerInteraction.Ignore))
                    continue;
                if (IsOnScreen(hit.position))
                    continue;
                if (!IsReachable(origin, hit.position))
                    continue;

                position = hit.position;
                return true;
            }

            position = default;
            return false;
        }

        /// <summary>
        /// True when the player can walk from <paramref name="from"/> to <paramref name="to"/>; the arena has
        /// walled-off pockets of NavMesh.
        /// </summary>
        private bool IsReachable(Vector3 from, Vector3 to)
        {
            if (!NavMesh.SamplePosition(from, out var start, 2f, NavMesh.AllAreas))
                return true;
            _path ??= new NavMeshPath();
            return NavMesh.CalculatePath(start.position, to, NavMesh.AllAreas, _path) && _path.status == NavMeshPathStatus.PathComplete;
        }

        /// <summary>Whether any part of a chest standing at <paramref name="position"/> would show on screen.</summary>
        private bool IsOnScreen(Vector3 position)
        {
            if (_camera == null)
                _camera = Camera.main;
            if (_camera == null)
                return false;

            // The chest's base and top: on a tilted camera either can be the one that peeks in.
            return IsInView(position) || IsInView(position + Vector3.up * 1.2f);
        }

        /// <summary>
        /// True when <paramref name="point"/> projects inside the screen, widened by the off-screen margin.
        /// </summary>
        private bool IsInView(Vector3 point)
        {
            var viewport = _camera.WorldToViewportPoint(point);
            return viewport.z > 0f
                   && viewport.x > -offscreenMargin && viewport.x < 1f + offscreenMargin
                   && viewport.y > -offscreenMargin && viewport.y < 1f + offscreenMargin;
        }
    }
}
