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

        private void OnEnable()
        {
            _root = GetComponent<UIDocument>().rootVisualElement;
            _root.RegisterCallback<ClickEvent>(OnClick, TrickleDown.TrickleDown);
            _root.RegisterCallback<PointerOverEvent>(OnPointerOver, TrickleDown.TrickleDown);
        }

        private void OnDisable()
        {
            _root.UnregisterCallback<ClickEvent>(OnClick, TrickleDown.TrickleDown);
            _root.UnregisterCallback<PointerOverEvent>(OnPointerOver, TrickleDown.TrickleDown);
        }

        private static void OnClick(ClickEvent evt)
        {
            if (FindButton(evt.target as VisualElement) != null)
                AudioManager.PlayClick();
        }

        // PointerOver fires for every child the pointer crosses, so only a change of button counts as a hover.
        private void OnPointerOver(PointerOverEvent evt)
        {
            var button = FindButton(evt.target as VisualElement);
            if (button == _hovered)
                return;

            _hovered = button;
            if (button != null && button.enabledInHierarchy)
                AudioManager.PlayHover();
        }

        private static Button FindButton(VisualElement element)
        {
            for (; element != null; element = element.parent)
                if (element is Button button)
                    return button;
            return null;
        }
    }
}
