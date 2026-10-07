using BattleArena.Player;
using UnityEngine.UIElements;

namespace BattleArena.UI.Elements
{
    /// <summary>Something the player can do, as shown in a button prompt.</summary>
    public enum ControlAction
    {
        Move,
        /// <summary>All three spell buttons in a row.</summary>
        Spells,
        Spell1,
        Spell2,
        Spell3,
        Interact,
        Pause,
        /// <summary>Leave a menu: Esc on a keyboard, B / Circle on a gamepad.</summary>
        Back,
        /// <summary>Move between level-up cards.</summary>
        Browse,
        /// <summary>Pick the highlighted card.</summary>
        Confirm
    }

    /// <summary>
    /// The button(s) for one <see cref="ControlAction"/>, drawn for the device the player is using: keycaps on a
    /// keyboard, face buttons on a gamepad (A/B/X/Y letters, or PlayStation symbols on a DualShock or DualSense).
    /// Rebuilds itself whenever <see cref="ControlScheme"/> changes. Looks come from the style sheet: keycaps are
    /// <c>.keycap</c> (plus <see cref="KeycapClass"/>), gamepad buttons <c>.pad-glyph</c> with a class per button.
    /// </summary>
    [UxmlElement]
    public partial class ControlGlyph : VisualElement
    {
        private ControlAction _action;
        private string _keycapClass;
        private ControlDevice _builtFor;
        private bool _built;

        [UxmlAttribute]
        public ControlAction Action
        {
            get => _action;
            set
            {
                _action = value;
                Rebuild();
            }
        }

        /// <summary>Extra classes for the keyboard keycaps, e.g. <c>keycap-accent</c>.</summary>
        [UxmlAttribute]
        public string KeycapClass
        {
            get => _keycapClass;
            set
            {
                _keycapClass = value;
                Rebuild();
            }
        }

        public ControlGlyph()
        {
            pickingMode = PickingMode.Ignore;
            AddToClassList("control-glyph");
            RegisterCallback<AttachToPanelEvent>(_ =>
            {
                ControlScheme.Changed += OnSchemeChanged;
                if (!_built || _builtFor != ControlScheme.Current)
                    Rebuild();
            });
            RegisterCallback<DetachFromPanelEvent>(_ => ControlScheme.Changed -= OnSchemeChanged);
            Rebuild();
        }

        private void OnSchemeChanged(ControlDevice _) => Rebuild();

        private void Rebuild()
        {
            Clear();
            _builtFor = ControlScheme.Current;
            _built = true;
            if (_builtFor == ControlDevice.KeyboardMouse)
                BuildKeyboard();
            else
                BuildGamepad(_builtFor == ControlDevice.PlayStation);
        }

        // ---------------------------------------------------------------- Keyboard

        private void BuildKeyboard()
        {
            switch (_action)
            {
                case ControlAction.Move:
                    Keys("W", "A", "S", "D");
                    break;
                case ControlAction.Spells:
                    Keys("Q", "E", "R");
                    break;
                case ControlAction.Spell1:
                    Keys("Q");
                    break;
                case ControlAction.Spell2:
                    Keys("E");
                    break;
                case ControlAction.Spell3:
                    Keys("R");
                    break;
                case ControlAction.Interact:
                case ControlAction.Confirm:
                    Keys("F");
                    break;
                case ControlAction.Pause:
                case ControlAction.Back:
                    Key("Esc").AddToClassList("keycap-wide");
                    break;
                case ControlAction.Browse:
                    Keys("A", "D");
                    var or = new Label("or") { pickingMode = PickingMode.Ignore };
                    or.AddToClassList("nav-hint-or");
                    Add(or);
                    ArrowKey("icon-arrow-left");
                    ArrowKey("icon-arrow-right");
                    break;
            }
        }

        private void Keys(params string[] keys)
        {
            foreach (var key in keys)
                Key(key);
        }

        private Label Key(string text)
        {
            var key = new Label(text) { pickingMode = PickingMode.Ignore };
            StyleKeycap(key);
            Add(key);
            return key;
        }

        private void ArrowKey(string iconClass)
        {
            var key = new VisualElement { pickingMode = PickingMode.Ignore };
            StyleKeycap(key);
            var icon = new VisualElement { pickingMode = PickingMode.Ignore };
            icon.AddToClassList("icon");
            icon.AddToClassList(iconClass);
            icon.AddToClassList("keycap-icon");
            key.Add(icon);
            Add(key);
        }

        private void StyleKeycap(VisualElement key)
        {
            key.AddToClassList("keycap");
            if (string.IsNullOrEmpty(_keycapClass))
                return;
            foreach (var extra in _keycapClass.Split(' ', System.StringSplitOptions.RemoveEmptyEntries))
                key.AddToClassList(extra);
        }

        // ---------------------------------------------------------------- Gamepad

        private void BuildGamepad(bool playStation)
        {
            switch (_action)
            {
                case ControlAction.Move:
                    Ring("pad-icon-stick");
                    break;
                case ControlAction.Spells:
                    Face(FaceButton.West, playStation);
                    Face(FaceButton.North, playStation);
                    Face(FaceButton.East, playStation);
                    break;
                case ControlAction.Spell1:
                    Face(FaceButton.West, playStation);
                    break;
                case ControlAction.Spell2:
                    Face(FaceButton.North, playStation);
                    break;
                case ControlAction.Spell3:
                case ControlAction.Back:
                    Face(FaceButton.East, playStation);
                    break;
                case ControlAction.Interact:
                case ControlAction.Confirm:
                    Face(FaceButton.South, playStation);
                    break;
                case ControlAction.Pause:
                    Ring("pad-icon-menu");
                    break;
                case ControlAction.Browse:
                    Ring("pad-icon-dpad");
                    break;
            }
        }

        private enum FaceButton
        {
            South,
            East,
            West,
            North
        }

        // Xbox letters and PlayStation symbols for the four face buttons, by position.
        private static readonly string[] XboxLetters = { "A", "B", "X", "Y" };
        private static readonly string[] PlayStationSymbols = { "cross", "circle", "square", "triangle" };

        private void Face(FaceButton button, bool playStation)
        {
            var glyph = new VisualElement { pickingMode = PickingMode.Ignore };
            glyph.AddToClassList("pad-glyph");
            glyph.AddToClassList("pad-face");
            if (playStation)
            {
                var symbol = PlayStationSymbols[(int)button];
                glyph.AddToClassList("pad-ps");
                glyph.AddToClassList("pad-ps-" + symbol);
                var icon = new VisualElement { pickingMode = PickingMode.Ignore };
                icon.AddToClassList("pad-symbol");
                icon.AddToClassList("pad-icon-" + symbol);
                glyph.Add(icon);
            }
            else
            {
                var letter = XboxLetters[(int)button];
                glyph.AddToClassList("pad-xbox");
                glyph.AddToClassList("pad-xbox-" + letter.ToLowerInvariant());
                var label = new Label(letter) { pickingMode = PickingMode.Ignore };
                label.AddToClassList("display");
                label.AddToClassList("pad-letter");
                glyph.Add(label);
            }
            Add(glyph);
        }

        private void Ring(string iconClass)
        {
            var glyph = new VisualElement { pickingMode = PickingMode.Ignore };
            glyph.AddToClassList("pad-glyph");
            glyph.AddToClassList("pad-ring");
            var icon = new VisualElement { pickingMode = PickingMode.Ignore };
            icon.AddToClassList("pad-symbol");
            icon.AddToClassList(iconClass);
            glyph.Add(icon);
            Add(glyph);
        }
    }
}
