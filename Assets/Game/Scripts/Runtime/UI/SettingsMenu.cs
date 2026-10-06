using BattleArena.Audio;
using BattleArena.Player;
using UnityEngine;
using UnityEngine.UIElements;

namespace BattleArena.UI
{
    /// <summary>
    /// The settings dialog over the main and pause menus: sound effect and ambience volume sliders and a credit line.
    ///
    /// The sliders drive AudioManager live, so the player hears what they are dragging. Saving is deliberately not
    /// done per tick (PlayerPrefs writes to disk, and a drag would write once per pixel); it happens once on close.
    /// Loading is not done here at all: AudioManager reads the prefs when it starts, so audio is already at the saved
    /// level and this only shows what it finds.
    ///
    /// Closes with Done, the X, a click outside the panel, or Esc (routed here by <see cref="MenuController"/>).
    /// Both menus it opens from already have the game stopped, so it needs no pause of its own.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class SettingsMenu : MonoBehaviour
    {
        // Focus goes back to the button that opened the dialog only after this delay, so the Enter press that closed
        // it is not delivered to that button as well and reopen the dialog.
        private const long RefocusDelayMs = 80;

        [SerializeField] private InputReader input;
        [SerializeField] private string author = "Created by Rafael Paz";

        private VisualElement _screen;
        private Slider _sfxSlider;
        private Slider _ambienceSlider;
        private Label _sfxValue;
        private Label _ambienceValue;
        private Label _credit;
        private Button _closeButton;
        private Button _doneButton;
        private Focusable _returnFocus;
        private bool _enabledSystemInput;

        public bool IsOpen { get; private set; }

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>().rootVisualElement;
            _screen = root.Q("settings-menu");
            _sfxSlider = root.Q<Slider>("sfx-slider");
            _ambienceSlider = root.Q<Slider>("ambience-slider");
            _sfxValue = root.Q<Label>("sfx-value");
            _ambienceValue = root.Q<Label>("ambience-value");
            _credit = root.Q<Label>("settings-credit");
            _closeButton = root.Q<Button>("settings-close");
            _doneButton = root.Q<Button>("settings-done");

            _closeButton.clicked += Close;
            _doneButton.clicked += Close;
            _sfxSlider.RegisterValueChangedCallback(OnSfxChanged);
            _ambienceSlider.RegisterValueChangedCallback(OnAmbienceChanged);
            _screen.RegisterCallback<PointerDownEvent>(OnBackdropPointerDown);

            // From the build's own version, so it cannot drift from Player Settings the way a typed string would.
            _credit.text = $"v{Application.version}  ·  {author}";
        }

        private void OnDisable()
        {
            _closeButton.clicked -= Close;
            _doneButton.clicked -= Close;
            _sfxSlider.UnregisterValueChangedCallback(OnSfxChanged);
            _ambienceSlider.UnregisterValueChangedCallback(OnAmbienceChanged);
            _screen.UnregisterCallback<PointerDownEvent>(OnBackdropPointerDown);
            if (IsOpen)
                Close();
        }

        public void Open()
        {
            if (IsOpen)
                return;

            // Read fresh on every open: the manager is the authority on what is playing, not the slider's last position.
            _sfxSlider.SetValueWithoutNotify(AudioManager.SfxVolume);
            _ambienceSlider.SetValueWithoutNotify(AudioManager.AmbienceVolume);
            RefreshReadouts();

            _returnFocus = _screen.focusController?.focusedElement;
            _screen.EnableInClassList("hidden", false);
            IsOpen = true;
            _doneButton.Focus();

            // Over the main menu the pause key is off with the rest of the game's input; turn it on so Esc closes this.
            _enabledSystemInput = !input.IsSystemEnabled;
            if (_enabledSystemInput)
                input.EnableSystem();
        }

        public void Close()
        {
            if (!IsOpen)
                return;

            IsOpen = false;
            _screen.EnableInClassList("hidden", true);
            AudioManager.SaveVolumePrefs();

            if (_enabledSystemInput)
                input.DisableSystem();
            _enabledSystemInput = false;

            var target = _returnFocus;
            _returnFocus = null;
            if (target != null)
                _screen.schedule.Execute(() => target.Focus()).StartingIn(RefocusDelayMs);
        }

        // Only a press on the dim backdrop itself; presses on the panel or its controls land on those instead.
        private void OnBackdropPointerDown(PointerDownEvent evt)
        {
            if (evt.target == _screen)
                Close();
        }

        private void OnSfxChanged(ChangeEvent<float> evt)
        {
            AudioManager.SetSfxVolume(evt.newValue);
            RefreshReadouts();
            // A level is easier to judge against a sound than a number; the click's rate limit keeps a drag from buzzing.
            AudioManager.PlayClick();
        }

        private void OnAmbienceChanged(ChangeEvent<float> evt)
        {
            AudioManager.SetAmbienceVolume(evt.newValue);
            RefreshReadouts();
        }

        private void RefreshReadouts()
        {
            _sfxValue.text = Mathf.RoundToInt(AudioManager.SfxVolume * 100f) + "%";
            _ambienceValue.text = Mathf.RoundToInt(AudioManager.AmbienceVolume * 100f) + "%";
        }
    }
}
