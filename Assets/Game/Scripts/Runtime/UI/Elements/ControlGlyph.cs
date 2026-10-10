using BattleArena.Player;
using UnityEngine.UIElements;

namespace BattleArena.UI.Elements
{
    /// <summary>
    /// The button(s) for one <see cref="ControlAction"/>, drawn for the device the player is using: keycaps on a
    /// keyboard, face buttons on a gamepad (A/B/X/Y letters, or PlayStation symbols on a DualShock or DualSense).
    /// Rebuilds itself whenever <see cref="ControlScheme"/> changes. Looks come from the style sheet: keycaps are
    /// <c>.keycap</c> (plus <see cref="KeycapClass"/>), gamepad buttons <c>.pad-glyph</c> with a class per button.
    /// </summary>
    [UxmlElement]
    public partial class ControlGlyph : VisualElement
    {
        /// <summary>Xbox letters for the four face buttons, by <see cref="FaceButton"/>.</summary>
        private static readonly string[] XboxLetters = { "A", "B", "X", "Y" };
        /// <summary>PlayStation symbols for the four face buttons, by <see cref="FaceButton"/>.</summary>
        private static readonly string[] PlayStationSymbols = { "cross", "circle", "square", "triangle" };

        private ControlAction _action;
        private string _keycapClass;
        private ControlDevice _builtFor;
        private bool _built;

        /// <summary>The action whose button(s) the glyph shows.</summary>
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

        /// <summary>Creates the glyph and follows the control scheme while it is on a panel.</summary>
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

        /// <summary>Rebuilds for the device the player switched to.</summary>
        private void OnSchemeChanged(ControlDevice _) => Rebuild();

        /// <summary>Replaces the children with keycaps or gamepad buttons for the current device.</summary>
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

        /// <summary>Adds the keycaps for the action.</summary>
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

        /// <summary>Adds one keycap per key name.</summary>
        private void Keys(params string[] keys)
        {
            foreach (var key in keys)
                Key(key);
        }

        /// <summary>Adds a keycap labelled <paramref name="text"/>.</summary>
        private Label Key(string text)
        {
            var key = new Label(text) { pickingMode = PickingMode.Ignore };
            StyleKeycap(key);
            Add(key);
            return key;
        }

        /// <summary>Adds a keycap showing an arrow icon.</summary>
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

        /// <summary>Applies the keycap classes, including any extra ones from <see cref="KeycapClass"/>.</summary>
        private void StyleKeycap(VisualElement key)
        {
            key.AddToClassList("keycap");
            if (string.IsNullOrEmpty(_keycapClass))
                return;
            foreach (var extra in _keycapClass.Split(' ', System.StringSplitOptions.RemoveEmptyEntries))
                key.AddToClassList(extra);
        }

        // ---------------------------------------------------------------- Gamepad

        /// <summary>Adds the gamepad buttons for the action.</summary>
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

        /// <summary>The four face buttons by position; indexes the letter and symbol tables.</summary>
        private enum FaceButton
        {
            /// <summary>Bottom button (A, cross).</summary>
            South,
            /// <summary>Right button (B, circle).</summary>
            East,
            /// <summary>Left button (X, square).</summary>
            West,
            /// <summary>Top button (Y, triangle).</summary>
            North
        }

        /// <summary>
        /// Adds a face button: an Xbox letter, or the PlayStation symbol when <paramref name="playStation"/>.
        /// </summary>
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

        /// <summary>Adds a round gamepad glyph with an icon (stick, d-pad, menu button).</summary>
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
