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

        /// <summary>The current phase of the session.</summary>
        public GameState State { get; private set; } = GameState.MainMenu;
        /// <summary>Seconds since the match started; frozen when it ends.</summary>
        public float ElapsedTime { get; private set; }
        /// <summary>Enemies killed this match.</summary>
        public int Kills { get; private set; }
        /// <summary>Seconds of survival before the boss arrives.</summary>
        public float MatchDuration => matchDuration;

        /// <summary>Raised with the new kill count after every kill.</summary>
        public event Action<int> KillsChanged;

        /// <summary>Gets the pause key before the game toggles pause (the menus, for the settings dialog).</summary>
        public IPauseKeyHandler PauseKeyHandler { get; set; }

        /// <summary>Subscribes to input, the player's death and enemy kills.</summary>
        private void OnEnable()
        {
            input.PauseRequested += OnPauseKey;
            input.BackRequested += OnBackKey;
            playerHealth.Died += OnPlayerDied;
            EventBus<EnemyKilledEvent>.Subscribe(OnEnemyKilled);
        }

        /// <summary>Removes every subscription and disables input.</summary>
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

        /// <summary>Resets the clock, disables input until the match starts and announces the initial state.</summary>
        private void Start()
        {
            Time.timeScale = 1f;
            input.DisableAll();
            EventBus<GameStateChangedEvent>.Raise(new GameStateChangedEvent(State, State));
        }

        /// <summary>
        /// Starts a match from the main menu: enables gameplay input, starts the spawner and the match clock.
        /// </summary>
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

        /// <summary>Lets the menus handle the pause key first (e.g. to close a dialog), then toggles pause.</summary>
        private void OnPauseKey(bool viaEnter)
        {
            if (PauseKeyHandler != null && PauseKeyHandler.HandlePauseKey(viaEnter))
                return;
            TogglePause();
        }

        /// <summary>
        /// Handles the back key: closes an open dialog first, like Esc; otherwise only leaves the pause menu, never
        /// enters it.
        /// </summary>
        private void OnBackKey()
        {
            if (PauseKeyHandler != null && PauseKeyHandler.HandlePauseKey(false))
                return;
            if (State == GameState.Paused)
                TogglePause();
        }

        /// <summary>
        /// Pauses combat or resumes it; ignored right after a state change so one press cannot do both.
        /// </summary>
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

        /// <summary>Resumes combat after the level-up cards.</summary>
        public void EndLevelUp()
        {
            if (State == GameState.LevelUp)
                Resume();
        }

        /// <summary>
        /// Freezes time and gameplay input under an overlay state, remembering the state to return to.
        /// </summary>
        private void Suspend(GameState overlay)
        {
            _stateBeforeSuspend = State;
            Time.timeScale = 0f;
            input.DisableGameplay();
            SetState(overlay);
        }

        /// <summary>Unfreezes time and input and returns to the state before the overlay.</summary>
        private void Resume()
        {
            Time.timeScale = 1f;
            input.EnableGameplay();
            SetState(_stateBeforeSuspend);
        }

        /// <summary>Reloads the scene for a fresh session.</summary>
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

        /// <summary>
        /// Publishes the match clock a few times a second and starts the boss fight when time runs out.
        /// </summary>
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

        /// <summary>Stops the regular waves, spawns the boss and announces it.</summary>
        private void BeginBossFight()
        {
            spawner.EnterBossFight();

            var boss = spawner.SpawnBoss();
            if (boss.TryGetComponent(out BossController controller))
                controller.SetTarget(playerHealth.transform);

            _bossHealth = boss.Health;
            _bossHealth.Died += OnBossDied;

            SetState(GameState.BossFight);
            EventBus<BossSpawnedEvent>.Raise(new BossSpawnedEvent(spawner.BossDefinition.DisplayName, _bossHealth));
            EventBus<NotificationEvent>.Raise(new NotificationEvent($"{spawner.BossDefinition.DisplayName} has awakened!"));
        }

        /// <summary>Counts a kill.</summary>
        private void OnEnemyKilled(EnemyKilledEvent _)
        {
            Kills++;
            KillsChanged?.Invoke(Kills);
        }

        /// <summary>Ends the match in victory.</summary>
        private void OnBossDied(Health health)
        {
            health.Died -= OnBossDied;
            _bossHealth = null;
            EndMatch(GameState.Victory);
        }

        /// <summary>Ends the match in defeat.</summary>
        private void OnPlayerDied(Health _) => EndMatch(GameState.Defeat);

        /// <summary>
        /// Stops the clock, the spawner and input, then shows the end screen. Only the first result counts.
        /// </summary>
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

        /// <summary>
        /// Plays the final moment in slow motion, then freezes time and switches to the result state.
        /// </summary>
        private IEnumerator ShowEndScreen(GameState result)
        {
            // Brief slow motion so the final blow reads before the end screen appears.
            Time.timeScale = 0.3f;
            yield return new WaitForSecondsRealtime(endScreenDelay);
            Time.timeScale = 0f;
            SetState(result);
        }

        /// <summary>Changes state and announces it on the event bus.</summary>
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
