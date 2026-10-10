using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.LowLevel;

namespace BattleArena.Player
{
    /// <summary>
    /// Tracks the device the player last used, so prompts can show keys or gamepad buttons to match. Only real input
    /// switches it: a pressed key or button, a stick pushed past a deadzone, or the mouse moved, never stick drift.
    /// </summary>
    public static class ControlScheme
    {
        private const float ChangeThreshold = 0.4f;

        /// <summary>The device the player used most recently.</summary>
        public static ControlDevice Current { get; private set; } = ControlDevice.KeyboardMouse;

        /// <summary>Raised when the player switches to a different kind of device.</summary>
        public static event Action<ControlDevice> Changed;

        /// <summary>Clears the listener and state before play starts.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetState()
        {
            InputSystem.onEvent -= OnInputEvent;
            Current = ControlDevice.KeyboardMouse;
            Changed = null;
        }

        /// <summary>Starts watching input events.</summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize() => InputSystem.onEvent += OnInputEvent;

        /// <summary>
        /// Switches <see cref="Current"/> when a different kind of device produces real input (a control moved past the
        /// threshold).
        /// </summary>
        private static void OnInputEvent(InputEventPtr eventPtr, InputDevice device)
        {
            if (!eventPtr.IsA<StateEvent>() && !eventPtr.IsA<DeltaStateEvent>())
                return;

            ControlDevice target;
            switch (device)
            {
                case DualShockGamepad:
                    target = ControlDevice.PlayStation;
                    break;
                case Gamepad:
                    target = ControlDevice.Xbox;
                    break;
                case Keyboard:
                case Mouse:
                    target = ControlDevice.KeyboardMouse;
                    break;
                default:
                    return;
            }

            // Events from the device already in use are the common case and need no inspection.
            if (target == Current)
                return;

            foreach (var _ in eventPtr.EnumerateChangedControls(device, ChangeThreshold))
            {
                Current = target;
                Changed?.Invoke(target);
                return;
            }
        }
    }
}
