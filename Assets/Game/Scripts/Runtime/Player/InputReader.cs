using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace BattleArena.Player
{
    /// <summary>
    /// Translates Input System callbacks into plain C# events so gameplay code never polls input.
    /// Shared as an asset, so any component can listen without scene references to a PlayerInput.
    /// </summary>
    [CreateAssetMenu(fileName = "InputReader", menuName = "BattleArena/Input Reader")]
    public sealed class InputReader : ScriptableObject
    {
        public const int SpellSlotCount = 3;

        [SerializeField] private InputActionAsset actions;

        // Runtime cache, never serialized: a script reload saves and restores an asset's private fields, which would
        // bring back "initialized" with detached copies of the action maps that the Input System no longer tracks
        // ("Map must be contained in state"), and gameplay input would never enable again.
        [NonSerialized] private InputActionMap _gameplayMap;
        [NonSerialized] private InputActionMap _systemMap;
        [NonSerialized] private InputAction _move;
        [NonSerialized] private InputAction _interact;
        [NonSerialized] private InputAction _pause;
        [NonSerialized] private InputAction _back;
        [NonSerialized] private InputAction _choicePrevious;
        [NonSerialized] private InputAction _choiceNext;
        [NonSerialized] private InputAction _confirm;
        [NonSerialized] private readonly InputAction[] _spells = new InputAction[SpellSlotCount];
        [NonSerialized] private bool _initialized;

        public Vector2 Move { get; private set; }

        public event Action<Vector2> MoveChanged;
        public event Action<int> SpellRequested;
        public event Action InteractRequested;
        /// <summary>Esc, Enter or Start. True when it was Enter, which also presses the focused menu button.</summary>
        public event Action<bool> PauseRequested;
        /// <summary>B / Circle: backs out of menus. In play the same button casts a spell, so it never pauses.</summary>
        public event Action BackRequested;
        /// <summary>-1 / +1 to move the highlighted card (A/D, arrows, d-pad).</summary>
        public event Action<int> ChoiceNavigated;
        /// <summary>Confirms the highlighted card (F, Space, south button).</summary>
        public event Action ConfirmRequested;

        public void EnableGameplay()
        {
            Initialize();
            _gameplayMap.Enable();
            _systemMap.Enable();
        }

        public void DisableGameplay()
        {
            Initialize();
            _gameplayMap.Disable();
            SetMove(Vector2.zero);
        }

        /// <summary>
        /// Enables only the system map (pause key, card navigation) with gameplay left off: a dialog over the main
        /// menu uses it so Esc can close it.
        /// </summary>
        public void EnableSystem()
        {
            Initialize();
            _systemMap.Enable();
        }

        public void DisableSystem()
        {
            Initialize();
            _systemMap.Disable();
        }

        public bool IsSystemEnabled
        {
            get
            {
                Initialize();
                return _systemMap.enabled;
            }
        }

        /// <summary>Disables every map, e.g. on the end screen.</summary>
        public void DisableAll()
        {
            DisableGameplay();
            _systemMap.Disable();
        }

        private void Initialize()
        {
            if (_initialized)
                return;

            _gameplayMap = actions.FindActionMap("Gameplay", throwIfNotFound: true);
            _systemMap = actions.FindActionMap("System", throwIfNotFound: true);

            _move = _gameplayMap.FindAction("Move", throwIfNotFound: true);
            _interact = _gameplayMap.FindAction("Interact", throwIfNotFound: true);
            _pause = _systemMap.FindAction("Pause", throwIfNotFound: true);
            _back = _systemMap.FindAction("Back", throwIfNotFound: true);
            _choicePrevious = _systemMap.FindAction("ChoicePrevious", throwIfNotFound: true);
            _choiceNext = _systemMap.FindAction("ChoiceNext", throwIfNotFound: true);
            _confirm = _systemMap.FindAction("Confirm", throwIfNotFound: true);
            for (var i = 0; i < SpellSlotCount; i++)
                _spells[i] = _gameplayMap.FindAction($"Spell{i + 1}", throwIfNotFound: true);

            _move.performed += OnMove;
            _move.canceled += OnMove;
            _interact.performed += OnInteract;
            _pause.performed += OnPause;
            _back.performed += OnBack;
            _choicePrevious.performed += OnChoicePrevious;
            _choiceNext.performed += OnChoiceNext;
            _confirm.performed += OnConfirm;
            _spells[0].performed += OnSpell1;
            _spells[1].performed += OnSpell2;
            _spells[2].performed += OnSpell3;

            _initialized = true;
        }

        private void OnDisable()
        {
            if (!_initialized)
                return;

            _move.performed -= OnMove;
            _move.canceled -= OnMove;
            _interact.performed -= OnInteract;
            _pause.performed -= OnPause;
            _back.performed -= OnBack;
            _choicePrevious.performed -= OnChoicePrevious;
            _choiceNext.performed -= OnChoiceNext;
            _confirm.performed -= OnConfirm;
            _spells[0].performed -= OnSpell1;
            _spells[1].performed -= OnSpell2;
            _spells[2].performed -= OnSpell3;
            _gameplayMap.Disable();
            _systemMap.Disable();
            _initialized = false;
        }

        private void SetMove(Vector2 value)
        {
            if (value == Move)
                return;
            Move = value;
            MoveChanged?.Invoke(value);
        }

        private void OnMove(InputAction.CallbackContext context) => SetMove(context.ReadValue<Vector2>());
        private void OnInteract(InputAction.CallbackContext _) => InteractRequested?.Invoke();
        private void OnPause(InputAction.CallbackContext context) => PauseRequested?.Invoke(IsEnterKey(context.control));

        private void OnBack(InputAction.CallbackContext _) => BackRequested?.Invoke();

        private static bool IsEnterKey(InputControl control) => control is KeyControl { keyCode: Key.Enter or Key.NumpadEnter };
        private void OnChoicePrevious(InputAction.CallbackContext _) => ChoiceNavigated?.Invoke(-1);
        private void OnChoiceNext(InputAction.CallbackContext _) => ChoiceNavigated?.Invoke(1);
        private void OnConfirm(InputAction.CallbackContext _) => ConfirmRequested?.Invoke();
        private void OnSpell1(InputAction.CallbackContext _) => SpellRequested?.Invoke(0);
        private void OnSpell2(InputAction.CallbackContext _) => SpellRequested?.Invoke(1);
        private void OnSpell3(InputAction.CallbackContext _) => SpellRequested?.Invoke(2);
    }
}
