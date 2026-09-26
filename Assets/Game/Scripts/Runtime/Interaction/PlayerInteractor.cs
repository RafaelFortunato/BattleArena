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
        [SerializeField] private PlayerProgression progression;

        private readonly List<IInteractable> _inRange = new(4);
        private IInteractable _current;

        public Health Health => health;
        public PlayerProgression Progression => progression;

        private void OnEnable() => input.InteractRequested += OnInteractRequested;

        private void OnDisable()
        {
            input.InteractRequested -= OnInteractRequested;
            foreach (var interactable in _inRange)
                Unsubscribe(interactable);
            _inRange.Clear();
            SetCurrent(null);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.TryGetComponent(out IInteractable interactable) || _inRange.Contains(interactable))
                return;

            _inRange.Add(interactable);
            interactable.StateChanged += OnInteractableStateChanged;
            interactable.Removed += OnInteractableRemoved;
            RefreshCurrent();
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.TryGetComponent(out IInteractable interactable))
                Forget(interactable);
        }

        private void OnInteractableStateChanged(IInteractable _) => RefreshCurrent();

        private void OnInteractableRemoved(IInteractable interactable) => Forget(interactable);

        private void Forget(IInteractable interactable)
        {
            if (!_inRange.Remove(interactable))
                return;

            Unsubscribe(interactable);
            RefreshCurrent();
        }

        private void Unsubscribe(IInteractable interactable)
        {
            interactable.StateChanged -= OnInteractableStateChanged;
            interactable.Removed -= OnInteractableRemoved;
        }

        private void OnInteractRequested()
        {
            if (_current is not { CanInteract: true })
                return;

            _current.Interact(this);
            RefreshCurrent();
        }

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

        private void SetCurrent(IInteractable interactable)
        {
            if (_current == interactable)
                return;

            _current = interactable;
            EventBus<InteractionTargetChangedEvent>.Raise(new InteractionTargetChangedEvent(interactable));
        }
    }
}
