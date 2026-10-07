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
        [Tooltip("Real-time seconds after any state change during which pause toggles are ignored. Enter both pauses and " +
                 "presses the focused menu button, so one press must not start the match and pause it, or resume twice.")]
        [SerializeField, Min(0f)] private float pauseToggleGuard = 0.25f;

        private GameState _stateBeforeSuspend;
        private float _matchStartTime;
        private Coroutine _matchTimer;
        private Health _bossHealth;
        private bool _isEnding;
        private float _stateChangedAt = float.NegativeInfinity;

        public GameState State { get; private set; } = GameState.MainMenu;
        public float ElapsedTime { get; private set; }
        public int Kills { get; private set; }
        public float MatchDuration => matchDuration;

        public event Action<int> KillsChanged;

        /// <summary>Gets the pause key before the game toggles pause (the menus, for the settings dialog).</summary>
        public IPauseKeyHandler PauseKeyHandler { get; set; }

        private void OnEnable()
        {
            input.PauseRequested += OnPauseKey;
            input.BackRequested += OnBackKey;
            playerHealth.Died += OnPlayerDied;
            EventBus<EnemyKilledEvent>.Subscribe(OnEnemyKilled);
        }

        private void OnDisable()
        {
            input.PauseRequested -= OnPauseKey;
            input.BackRequested -= OnBackKey;
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

        private void OnPauseKey(bool viaEnter)
        {
            if (PauseKeyHandler != null && PauseKeyHandler.HandlePauseKey(viaEnter))
                return;
            TogglePause();
        }

        // Closes an open dialog first, like Esc; otherwise only leaves the pause menu, never enters it.
        private void OnBackKey()
        {
            if (PauseKeyHandler != null && PauseKeyHandler.HandlePauseKey(false))
                return;
            if (State == GameState.Paused)
                TogglePause();
        }

        public void TogglePause()
        {
            if (Time.unscaledTime - _stateChangedAt < pauseToggleGuard)
                return;

            if (State.IsCombat())
                Suspend(GameState.Paused);
            else if (State == GameState.Paused)
                Resume();
        }

        /// <summary>Freezes combat for the level-up cards. Returns false if the match isn't in combat.</summary>
        public bool BeginLevelUp()
        {
            if (!State.IsCombat())
                return State == GameState.LevelUp;

            Suspend(GameState.LevelUp);
            return true;
        }

        public void EndLevelUp()
        {
            if (State == GameState.LevelUp)
                Resume();
        }

        private void Suspend(GameState overlay)
        {
            _stateBeforeSuspend = State;
            Time.timeScale = 0f;
            input.DisableGameplay();
            SetState(overlay);
        }

        private void Resume()
        {
            Time.timeScale = 1f;
            input.EnableGameplay();
            SetState(_stateBeforeSuspend);
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
            _stateChangedAt = Time.unscaledTime;
            EventBus<GameStateChangedEvent>.Raise(new GameStateChangedEvent(previous, next));
        }
    }
}
