using UnityEngine;
using UnityEngine.UIElements;

namespace BattleArena.Audio
{
    /// <summary>
    /// Gives every <see cref="Button"/> in this document the shared click and hover sounds without the views that
    /// build them having to know about audio. Listens once on the root rather than per button, so buttons created
    /// later (level-up cards) are covered too. Clicks ride ClickEvent, which a disabled button never receives,
    /// so a refused press stays silent.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public sealed class UIClickSound : MonoBehaviour
    {
        private VisualElement _root;
        private Button _hovered;

        /// <summary>
        /// Listens for clicks and hovers on the document root, in the trickle-down phase so every button is covered.
        /// </summary>
        private void OnEnable()
        {
            _root = GetComponent<UIDocument>().rootVisualElement;
            _root.RegisterCallback<ClickEvent>(OnClick, TrickleDown.TrickleDown);
            _root.RegisterCallback<PointerOverEvent>(OnPointerOver, TrickleDown.TrickleDown);
        }

        /// <summary>Removes the root listeners.</summary>
        private void OnDisable()
        {
            _root.UnregisterCallback<ClickEvent>(OnClick, TrickleDown.TrickleDown);
            _root.UnregisterCallback<PointerOverEvent>(OnPointerOver, TrickleDown.TrickleDown);
        }

        /// <summary>Plays the click sound when the clicked element is, or sits inside, a button.</summary>
        private static void OnClick(ClickEvent evt)
        {
            if (FindButton(evt.target as VisualElement) != null)
                AudioManager.PlayClick();
        }

        /// <summary>
        /// Plays the hover sound when the pointer reaches a new enabled button. PointerOver fires for every child the
        /// pointer crosses, so only a change of button counts.
        /// </summary>
        private void OnPointerOver(PointerOverEvent evt)
        {
            var button = FindButton(evt.target as VisualElement);
            if (button == _hovered)
                return;

            _hovered = button;
            if (button != null && button.enabledInHierarchy)
                AudioManager.PlayHover();
        }

        /// <summary>
        /// The button that contains <paramref name="element"/> (or is it), or null when it is not part of a button.
        /// </summary>
        private static Button FindButton(VisualElement element)
        {
            for (; element != null; element = element.parent)
                if (element is Button button)
                    return button;
            return null;
        }
    }
}
