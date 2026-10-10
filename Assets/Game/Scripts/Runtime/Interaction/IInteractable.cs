using System;
using UnityEngine;

namespace BattleArena.Interaction
{
    /// <summary>Something the player can use with the Interact key (F) while standing next to it.</summary>
    public interface IInteractable
    {
        /// <summary>Short verb shown in the prompt, e.g. "Open chest".</summary>
        string Prompt { get; }

        /// <summary>True when using it now would do something (e.g. the well is full, the chest is closed).</summary>
        bool CanInteract { get; }

        /// <summary>World position, for picking the closest interactable.</summary>
        Vector3 Position { get; }

        /// <summary>Raised when <see cref="CanInteract"/> or <see cref="Prompt"/> changes.</summary>
        event Action<IInteractable> StateChanged;

        /// <summary>
        /// Raised when the interactable leaves the world (e.g. a despawned chest). Needed because
        /// Unity does not send OnTriggerExit for colliders that get deactivated.
        /// </summary>
        event Action<IInteractable> Removed;

        /// <summary>Uses the interactable on behalf of <paramref name="interactor"/>.</summary>
        void Interact(PlayerInteractor interactor);
    }
}
