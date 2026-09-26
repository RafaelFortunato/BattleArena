using System;
using UnityEngine;

namespace BattleArena.Interaction
{
    /// <summary>Something the player can use with the Interact key (F) while standing next to it.</summary>
    public interface IInteractable
    {
        /// <summary>Short verb shown in the prompt, e.g. "Open chest".</summary>
        string Prompt { get; }

        bool CanInteract { get; }

        Vector3 Position { get; }

        /// <summary>Raised when <see cref="CanInteract"/> or <see cref="Prompt"/> changes.</summary>
        event Action<IInteractable> StateChanged;

        void Interact(PlayerInteractor interactor);
    }
}
