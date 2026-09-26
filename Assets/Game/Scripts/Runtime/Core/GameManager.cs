using System;
using System.Collections;
using BattleArena.Combat;
using BattleArena.Enemies;
using BattleArena.Player;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BattleArena.Core
{
    /// <summary>
    /// Owns the match flow: main menu → 10 minute survival → boss fight → victory or defeat.
    /// Other systems react to <see cref="GameStateChangedEvent"/> instead of referencing this class.
    /// </summary>
    public sealed class GameManager : MonoBehaviour
    {
        private const float TimeEventInterval = 0.25f;

        [SerializeField] private InputReader input;
        [SerializeField] private Health playerHealth;
        [SerializeField] private EnemySpawner spawner;
        [SerializeField, Min(1f)] private float matchDuration = 600f;
        [SerializeField, Min(0f)] private float endScreenDelay = 1.5f;

        private GameState _stateBeforePause;
        private float _matchStartTime;
        private Coroutine _matchTimer;
        private Health _bossHealth;
        private bool _isEnding;

        public GameState State { get; private set; } = GameState.MainMenu;
        public float ElapsedTime { get; private set; }
        public int Kills { get; private set; }
        public float MatchDuration => matchDuration;

        public event Action<int> KillsChanged;

        private void OnEnable()
        {
            input.PauseRequested += TogglePause;
            playerHealth.Died += OnPlayerDied;
            EventBus<EnemyKilledEvent>.Subscribe(OnEnemyKilled);
        }

        private void OnDisable()
        {
            input.PauseRequested -= TogglePause;
            playerHealth.Died -= OnPlayerDied;
            EventBus<EnemyKilledEvent>.Unsubscribe(OnEnemyKilled);
            if (_bossHealth != null)
                _bossHealth.Died -= OnBossDied;
            input.DisableAll();
        }

        private void Start()
        {
            Time.timeScale = 1f;
            input.DisableAll();
            EventBus<GameStateChangedEvent>.Raise(new GameStateChangedEvent(State, State));
        }

        public void StartMatch()
        {
            if (State != GameState.MainMenu)
                return;

            _matchStartTime = Time.time;
            Kills = 0;
            KillsChanged?.Invoke(Kills);
            input.EnableGameplay();
            spawner.Begin(_matchStartTime);
            _matchTimer = StartCoroutine(MatchTimer());
            SetState(GameState.Playing);
        }

        public void TogglePause()
        {
            if (State.IsCombat())
            {
                _stateBeforePause = State;
                Time.timeScale = 0f;
                input.DisableGameplay();
                SetState(GameState.Paused);
            }
            else if (State == GameState.Paused)
            {
                Time.timeScale = 1f;
                input.EnableGameplay();
                SetState(_stateBeforePause);
            }
        }

        public void Restart()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        /// <summary>Debug shortcut to test the boss fight without waiting ten minutes.</summary>
        [ContextMenu("Skip To Boss")]
        public void SkipToBoss()
        {
            if (State == GameState.Playing)
                _matchStartTime = Time.time - matchDuration;
        }

        private IEnumerator MatchTimer()
        {
            var wait = WaitCache.Get(TimeEventInterval);
            while (true)
            {
                ElapsedTime = Time.time - _matchStartTime;
                var remaining = Mathf.Max(0f, matchDuration - ElapsedTime);
                EventBus<MatchTimeChangedEvent>.Raise(new MatchTimeChangedEvent(ElapsedTime, remaining));

                if (remaining <= 0f)
                    break;
                yield return wait;
            }

            _matchTimer = null;
            BeginBossFight();
        }

        private void BeginBossFight()
        {
            spawner.StopSpawning();

            var boss = spawner.SpawnBoss();
            if (boss.TryGetComponent(out BossController controller))
                controller.SetTarget(playerHealth.transform);

            _bossHealth = boss.Health;
            _bossHealth.Died += OnBossDied;

            SetState(GameState.BossFight);
            EventBus<BossSpawnedEvent>.Raise(new BossSpawnedEvent(spawner.BossDefinition.DisplayName, _bossHealth));
            EventBus<NotificationEvent>.Raise(new NotificationEvent($"{spawner.BossDefinition.DisplayName} has awakened!"));
        }

        private void OnEnemyKilled(EnemyKilledEvent _)
        {
            Kills++;
            KillsChanged?.Invoke(Kills);
        }

        private void OnBossDied(Health health)
        {
            health.Died -= OnBossDied;
            _bossHealth = null;
            EndMatch(GameState.Victory);
        }

        private void OnPlayerDied(Health _) => EndMatch(GameState.Defeat);

        private void EndMatch(GameState result)
        {
            if (_isEnding || !State.IsCombat())
                return;

            _isEnding = true;
            if (_matchTimer != null)
            {
                StopCoroutine(_matchTimer);
                _matchTimer = null;
            }

            ElapsedTime = Time.time - _matchStartTime;
            spawner.StopSpawning();
            input.DisableAll();
            StartCoroutine(ShowEndScreen(result));
        }

        private IEnumerator ShowEndScreen(GameState result)
        {
            // Brief slow motion so the final blow reads before the end screen appears.
            Time.timeScale = 0.3f;
            yield return new WaitForSecondsRealtime(endScreenDelay);
            Time.timeScale = 0f;
            SetState(result);
        }

        private void SetState(GameState next)
        {
            if (State == next)
                return;

            var previous = State;
            State = next;
            EventBus<GameStateChangedEvent>.Raise(new GameStateChangedEvent(previous, next));
        }
    }
}
