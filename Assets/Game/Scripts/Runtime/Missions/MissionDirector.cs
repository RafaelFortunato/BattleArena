using System.Collections;
using BattleArena.Combat;
using BattleArena.Core;
using BattleArena.Enemies;
using BattleArena.Progression;
using UnityEngine;

namespace BattleArena.Missions
{
    /// <summary>
    /// Offers missions from time to time once the player has a spell, one at a time. Success queues a
    /// spell upgrade choice. Missions stop when the boss arrives.
    /// </summary>
    public sealed class MissionDirector : MonoBehaviour
    {
        [SerializeField] private MissionDefinition[] missions;
        [SerializeField] private LevelUpController rewards;
        [SerializeField] private Transform player;
        [SerializeField] private Health playerHealth;
        [SerializeField] private EnemySpawner spawner;
        [SerializeField] private GameManager gameManager;
        [SerializeField] private MissionPointer pointer;

        [Header("Pacing")]
        [SerializeField, Min(0f)] private float firstMissionDelay = 15f;
        [SerializeField, Min(1f)] private float minInterval = 35f;
        [SerializeField, Min(1f)] private float maxInterval = 55f;
        [SerializeField, Range(0.05f, 1f)] private float tickInterval = 0.1f;

        private MissionContext _context;
        private MissionInstance _active;
        private MissionDefinition _previous;
        private Coroutine _loop;

        /// <summary>Builds the context shared by every mission.</summary>
        private void Awake() => _context = new MissionContext(player, playerHealth, spawner, gameManager);

        /// <summary>Listens for the first spell and for match state changes.</summary>
        private void OnEnable()
        {
            EventBus<SpellEquippedEvent>.Subscribe(OnSpellEquipped);
            EventBus<GameStateChangedEvent>.Subscribe(OnGameStateChanged);
        }

        /// <summary>Stops listening and ends any running mission quietly.</summary>
        private void OnDisable()
        {
            EventBus<SpellEquippedEvent>.Unsubscribe(OnSpellEquipped);
            EventBus<GameStateChangedEvent>.Unsubscribe(OnGameStateChanged);
            EndActive(announce: false);
            _loop = null;
        }

        /// <summary>Starts offering missions once the player has their first spell.</summary>
        private void OnSpellEquipped(SpellEquippedEvent _) => _loop ??= StartCoroutine(MissionLoop());

        /// <summary>
        /// Stops offering missions when the boss arrives or the match ends, abandoning the one in progress.
        /// </summary>
        private void OnGameStateChanged(GameStateChangedEvent evt)
        {
            if (evt.Current is not (GameState.BossFight or GameState.Victory or GameState.Defeat))
                return;

            if (_loop != null)
            {
                StopCoroutine(_loop);
                // Keep the field set so a later spell doesn't restart the loop.
            }
            if (_active != null)
            {
                if (evt.Current == GameState.BossFight)
                    EventBus<NotificationEvent>.Raise(new NotificationEvent("Mission abandoned: the boss has arrived!"));
                EndActive(announce: false);
            }
        }

        /// <summary>
        /// Runs missions one after another: ticks the active one, reports its progress, then waits a random interval
        /// before the next.
        /// </summary>
        private IEnumerator MissionLoop()
        {
            yield return WaitCache.Get(firstMissionDelay);
            var tick = WaitCache.Get(tickInterval);

            while (true)
            {
                StartMission(PickMission());

                var lastTick = Time.time;
                while (_active != null)
                {
                    yield return tick;
                    var now = Time.time;
                    _active.Tick(now, now - lastTick);
                    lastTick = now;

                    if (_active.Status == MissionStatus.Running)
                        Report();
                    else
                        Finish();
                }

                // Allocates once per mission; negligible, and keeps intervals truly random.
                yield return new WaitForSeconds(Random.Range(minInterval, maxInterval));
            }
        }

        /// <summary>Picks a mission at random by weight, never the same one twice in a row.</summary>
        private MissionDefinition PickMission()
        {
            var total = 0f;
            foreach (var mission in missions)
                if (mission != _previous)
                    total += mission.Weight;

            var roll = Random.value * total;
            foreach (var mission in missions)
            {
                if (mission == _previous)
                    continue;
                roll -= mission.Weight;
                if (roll <= 0f)
                    return mission;
            }
            return missions[0];
        }

        /// <summary>Starts a mission and announces it; the HUD shows it from the announcement.</summary>
        private void StartMission(MissionDefinition definition)
        {
            // Never leave a previous mission's objects behind.
            EndActive(announce: false);

            _previous = definition;
            _active = definition.CreateInstance(_context);
            _active.Begin();

            // The HUD announces the mission itself from this event.
            EventBus<MissionStartedEvent>.Raise(new MissionStartedEvent(definition.Title, _active.Description));
            Report();
        }

        /// <summary>Publishes the active mission's progress and aims the objective pointer.</summary>
        private void Report()
        {
            EventBus<MissionProgressEvent>.Raise(new MissionProgressEvent(_active.Progress, _active.ProgressText, _active.TimeRemaining));
            pointer.SetTarget(_active.TryGetTarget(out var target), target);
        }

        /// <summary>Ends the active mission and queues the reward on success or announces the failure.</summary>
        private void Finish()
        {
            var succeeded = _active.Status == MissionStatus.Succeeded;
            var title = _active.Definition.Title;
            EndActive(announce: true);

            if (succeeded)
                rewards.QueueSpellUpgradeReward(title);
            else
                EventBus<NotificationEvent>.Raise(new NotificationEvent($"Mission failed: {title}"));
        }

        /// <summary>Cleans up the active mission, hides the pointer and announces the end.</summary>
        private void EndActive(bool announce)
        {
            if (_active == null)
                return;

            var succeeded = _active.Status == MissionStatus.Succeeded;
            _active.Cleanup();
            _active = null;
            pointer.SetTarget(false, default);
            EventBus<MissionEndedEvent>.Raise(new MissionEndedEvent(announce && succeeded));
        }
    }
}
