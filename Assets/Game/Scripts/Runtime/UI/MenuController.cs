using System.Globalization;
using BattleArena.Core;
using BattleArena.Player;
using BattleArena.Progression;
using UnityEngine;
using UnityEngine.UIElements;

namespace BattleArena.UI
{
    /// <summary>Main menu, pause menu and end screen. Screens are switched from game state changes.</summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class MenuController : MonoBehaviour
    {
        [SerializeField] private GameManager gameManager;
        [SerializeField] private PlayerProgression progression;
        [SerializeField] private SpellCaster spellCaster;
        [SerializeField] private PlayerStats stats;
        [SerializeField] private LevelUpConfig levelUpConfig;

        private VisualElement _mainMenu;
        private VisualElement _pauseMenu;
        private VisualElement _endScreen;
        private Label _pauseTime;
        private Label _pauseLevel;
        private Label _pauseKills;
        private Label _endTitle;
        private Label _endSubtitle;
        private VisualElement _endIcon;
        private Label _endTime;
        private Label _endLevel;
        private Label _endKills;
        private Button _startButton;
        private Button _resumeButton;
        private Button _pauseRestartButton;
        private Button _endRestartButton;
        private BuildSummaryView _build;

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            _mainMenu = root.Q("main-menu");
            _pauseMenu = root.Q("pause-menu");
            _endScreen = root.Q("end-screen");
            _pauseTime = root.Q<Label>("pause-time");
            _pauseLevel = root.Q<Label>("pause-level");
            _pauseKills = root.Q<Label>("pause-kills");
            _endTitle = root.Q<Label>("end-title");
            _endSubtitle = root.Q<Label>("end-subtitle");
            _endIcon = root.Q("end-icon");
            _endTime = root.Q<Label>("end-time");
            _endLevel = root.Q<Label>("end-level");
            _endKills = root.Q<Label>("end-kills");
            _startButton = root.Q<Button>("start-button");
            _resumeButton = root.Q<Button>("resume-button");
            _pauseRestartButton = root.Q<Button>("pause-restart-button");
            _endRestartButton = root.Q<Button>("end-restart-button");
            _build ??= new BuildSummaryView(root, spellCaster, stats, levelUpConfig);

            _startButton.clicked += gameManager.StartMatch;
            _resumeButton.clicked += gameManager.TogglePause;
            _pauseRestartButton.clicked += gameManager.Restart;
            _endRestartButton.clicked += gameManager.Restart;
            EventBus<GameStateChangedEvent>.Subscribe(OnGameStateChanged);
        }

        private void OnDisable()
        {
            _startButton.clicked -= gameManager.StartMatch;
            _resumeButton.clicked -= gameManager.TogglePause;
            _pauseRestartButton.clicked -= gameManager.Restart;
            _endRestartButton.clicked -= gameManager.Restart;
            EventBus<GameStateChangedEvent>.Unsubscribe(OnGameStateChanged);
        }

        private void OnGameStateChanged(GameStateChangedEvent evt)
        {
            var state = evt.Current;
            SetVisible(_mainMenu, state == GameState.MainMenu);
            SetVisible(_pauseMenu, state == GameState.Paused);
            SetVisible(_endScreen, state is GameState.Victory or GameState.Defeat);

            switch (state)
            {
                case GameState.MainMenu:
                    _startButton.Focus();
                    break;
                case GameState.Paused:
                    ShowPause();
                    break;
                case GameState.Victory:
                    ShowEndScreen(true, "VICTORY", "The arena is yours.");
                    break;
                case GameState.Defeat:
                    ShowEndScreen(false, "DEFEATED", "The horde overwhelmed you.");
                    break;
            }
        }

        private void ShowPause()
        {
            var remaining = Mathf.CeilToInt(gameManager.MatchDuration - gameManager.ElapsedTime);
            _pauseTime.text = remaining > 0
                ? $"{remaining / 60:00}:{remaining % 60:00} {Muted("to boss")}"
                : Muted("Boss fight");
            _pauseLevel.text = $"{Muted("Level")} {progression.Level}";
            _pauseKills.text = $"{gameManager.Kills.ToString("N0", CultureInfo.InvariantCulture)} {Muted("defeated")}";
            _build.Refresh();
            _resumeButton.Focus();
        }

        private void ShowEndScreen(bool victory, string title, string subtitle)
        {
            var seconds = Mathf.FloorToInt(gameManager.ElapsedTime);
            _endScreen.EnableInClassList("end-screen-defeat", !victory);
            _endIcon.EnableInClassList("icon-crown", victory);
            _endIcon.EnableInClassList("icon-skull", !victory);
            _endTitle.text = title;
            _endSubtitle.text = subtitle;
            _endTime.text = $"{seconds / 60:00}:{seconds % 60:00}";
            _endLevel.text = progression.Level.ToString();
            _endKills.text = gameManager.Kills.ToString("N0", CultureInfo.InvariantCulture);
            _endRestartButton.Focus();
        }

        private static string Muted(string text) => $"<color=#AEB6C8>{text}</color>";

        private static void SetVisible(VisualElement element, bool visible) => element.EnableInClassList("hidden", !visible);
    }
}
