using System.Collections.Generic;
using BattleArena.Combat;
using BattleArena.Core;
using BattleArena.Player;
using UnityEngine;

namespace BattleArena.Interaction
{
    /// <summary>
    /// Tracks interactables inside a trigger around the player and uses the closest available one on F.
    /// Selection is event-driven: it is only re-evaluated when something enters, leaves or changes state.
    /// </summary>
    [RequireComponent(typeof(SphereCollider))]
    public sealed class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] private InputReader input;
        [SerializeField] private Health health;

        private readonly List<IInteractable> _inRange = new(4);
        private IInteractable _current;

        /// <summary>The player's health, for interactables that heal.</summary>
        public Health Health => health;

        /// <summary>Listens for the interact key.</summary>
        private void OnEnable() => input.InteractRequested += OnInteractRequested;

        /// <summary>Stops listening and forgets everything in range.</summary>
        private void OnDisable()
        {
            input.InteractRequested -= OnInteractRequested;
            foreach (var interactable in _inRange)
                Unsubscribe(interactable);
            _inRange.Clear();
            SetCurrent(null);
        }

        /// <summary>Starts tracking an interactable that came into range.</summary>
        private void OnTriggerEnter(Collider other)
        {
            if (!other.TryGetComponent(out IInteractable interactable) || _inRange.Contains(interactable))
                return;

            _inRange.Add(interactable);
            interactable.StateChanged += OnInteractableStateChanged;
            interactable.Removed += OnInteractableRemoved;
            RefreshCurrent();
        }

        /// <summary>Stops tracking an interactable that left range.</summary>
        private void OnTriggerExit(Collider other)
        {
            if (other.TryGetComponent(out IInteractable interactable))
                Forget(interactable);
        }

        /// <summary>Re-picks the target when a tracked interactable becomes available or unavailable.</summary>
        private void OnInteractableStateChanged(IInteractable _) => RefreshCurrent();

        /// <summary>Forgets an interactable that left the world without a trigger exit.</summary>
        private void OnInteractableRemoved(IInteractable interactable) => Forget(interactable);

        /// <summary>Stops tracking an interactable and re-picks the target.</summary>
        private void Forget(IInteractable interactable)
        {
            if (!_inRange.Remove(interactable))
                return;

            Unsubscribe(interactable);
            RefreshCurrent();
        }

        /// <summary>Removes this interactor's listeners from an interactable.</summary>
        private void Unsubscribe(IInteractable interactable)
        {
            interactable.StateChanged -= OnInteractableStateChanged;
            interactable.Removed -= OnInteractableRemoved;
        }

        /// <summary>Uses the current target if it is available.</summary>
        private void OnInteractRequested()
        {
            if (_current is not { CanInteract: true })
                return;

            _current.Interact(this);
            RefreshCurrent();
        }

        /// <summary>Targets the closest available interactable in range.</summary>
        private void RefreshCurrent()
        {
            IInteractable best = null;
            var bestSqrDistance = float.MaxValue;
            var position = transform.position;

            foreach (var interactable in _inRange)
            {
                if (!interactable.CanInteract)
                    continue;

                var sqrDistance = (interactable.Position - position).sqrMagnitude;
                if (sqrDistance < bestSqrDistance)
                {
                    bestSqrDistance = sqrDistance;
                    best = interactable;
                }
            }

            SetCurrent(best);
        }

        /// <summary>Changes the target and announces it for the prompt.</summary>
        private void SetCurrent(IInteractable interactable)
        {
            if (_current == interactable)
                return;

            _current = interactable;
            EventBus<InteractionTargetChangedEvent>.Raise(new InteractionTargetChangedEvent(interactable));
        }
    }
}
