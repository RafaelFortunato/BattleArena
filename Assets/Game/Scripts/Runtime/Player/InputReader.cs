using System;
using UnityEngine;
using UnityEngine.InputSystem;

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
        public const int ChoiceCount = 3;

        [SerializeField] private InputActionAsset actions;

        private InputActionMap _gameplayMap;
        private InputActionMap _systemMap;
        private InputAction _move;
        private InputAction _interact;
        private InputAction _pause;
        private readonly InputAction[] _spells = new InputAction[SpellSlotCount];
        private readonly InputAction[] _choices = new InputAction[ChoiceCount];
        private bool _initialized;

        public Vector2 Move { get; private set; }

        public event Action<Vector2> MoveChanged;
        public event Action<int> SpellRequested;
        public event Action InteractRequested;
        public event Action PauseRequested;
        /// <summary>Card index (0-2) picked with the 1/2/3 keys on the level-up screen.</summary>
        public event Action<int> ChoiceRequested;

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
            for (var i = 0; i < SpellSlotCount; i++)
                _spells[i] = _gameplayMap.FindAction($"Spell{i + 1}", throwIfNotFound: true);
            for (var i = 0; i < ChoiceCount; i++)
                _choices[i] = _systemMap.FindAction($"Choice{i + 1}", throwIfNotFound: true);

            _move.performed += OnMove;
            _move.canceled += OnMove;
            _interact.performed += OnInteract;
            _pause.performed += OnPause;
            _spells[0].performed += OnSpell1;
            _spells[1].performed += OnSpell2;
            _spells[2].performed += OnSpell3;
            _choices[0].performed += OnChoice1;
            _choices[1].performed += OnChoice2;
            _choices[2].performed += OnChoice3;

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
            _spells[0].performed -= OnSpell1;
            _spells[1].performed -= OnSpell2;
            _spells[2].performed -= OnSpell3;
            _choices[0].performed -= OnChoice1;
            _choices[1].performed -= OnChoice2;
            _choices[2].performed -= OnChoice3;
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
        private void OnPause(InputAction.CallbackContext _) => PauseRequested?.Invoke();
        private void OnSpell1(InputAction.CallbackContext _) => SpellRequested?.Invoke(0);
        private void OnSpell2(InputAction.CallbackContext _) => SpellRequested?.Invoke(1);
        private void OnSpell3(InputAction.CallbackContext _) => SpellRequested?.Invoke(2);
        private void OnChoice1(InputAction.CallbackContext _) => ChoiceRequested?.Invoke(0);
        private void OnChoice2(InputAction.CallbackContext _) => ChoiceRequested?.Invoke(1);
        private void OnChoice3(InputAction.CallbackContext _) => ChoiceRequested?.Invoke(2);
    }
}
