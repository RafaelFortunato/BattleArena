using BattleArena.Core;
using BattleArena.Player;
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

        private VisualElement _mainMenu;
        private VisualElement _pauseMenu;
        private VisualElement _endScreen;
        private Label _endTitle;
        private Label _endSubtitle;
        private Label _endStats;
        private Button _startButton;
        private Button _resumeButton;
        private Button _pauseRestartButton;
        private Button _endRestartButton;

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            _mainMenu = root.Q("main-menu");
            _pauseMenu = root.Q("pause-menu");
            _endScreen = root.Q("end-screen");
            _endTitle = root.Q<Label>("end-title");
            _endSubtitle = root.Q<Label>("end-subtitle");
            _endStats = root.Q<Label>("end-stats");
            _startButton = root.Q<Button>("start-button");
            _resumeButton = root.Q<Button>("resume-button");
            _pauseRestartButton = root.Q<Button>("pause-restart-button");
            _endRestartButton = root.Q<Button>("end-restart-button");

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
                    _resumeButton.Focus();
                    break;
                case GameState.Victory:
                    ShowEndScreen("VICTORY", "The arena is yours.");
                    break;
                case GameState.Defeat:
                    ShowEndScreen("DEFEAT", "The horde overwhelmed you.");
                    break;
            }
        }

        private void ShowEndScreen(string title, string subtitle)
        {
            var seconds = Mathf.FloorToInt(gameManager.ElapsedTime);
            _endTitle.text = title;
            _endSubtitle.text = subtitle;
            _endStats.text = $"Time {seconds / 60:00}:{seconds % 60:00}  ·  Level {progression.Level}  ·  {gameManager.Kills} defeated";
            _endRestartButton.Focus();
        }

        private static void SetVisible(VisualElement element, bool visible) => element.EnableInClassList("hidden", !visible);
    }
}
