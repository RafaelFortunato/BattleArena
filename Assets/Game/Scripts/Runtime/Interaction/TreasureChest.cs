using System;
using System.Collections;
using BattleArena.Core;
using UnityEngine;

namespace BattleArena.Interaction
{
    /// <summary>Single-use chest that grants a burst of experience scaled to the player's level.</summary>
    public sealed class TreasureChest : MonoBehaviour, IInteractable
    {
        [SerializeField, Min(0)] private int baseExperience = 15;
        [SerializeField, Min(0)] private int experiencePerLevel = 4;
        [SerializeField] private Transform lid;
        [SerializeField] private float openAngle = -110f;
        [SerializeField, Min(0.01f)] private float openDuration = 0.4f;

        private bool _isOpen;

        public string Prompt => "Open chest";
        public bool CanInteract => !_isOpen;
        public Vector3 Position => transform.position;

        public event Action<IInteractable> StateChanged;

        public void Interact(PlayerInteractor interactor)
        {
            if (_isOpen)
                return;

            _isOpen = true;
            var progression = interactor.Progression;
            var reward = baseExperience + experiencePerLevel * progression.Level;
            progression.AddExperience(reward);

            EventBus<NotificationEvent>.Raise(new NotificationEvent($"Treasure! +{reward} XP"));
            StateChanged?.Invoke(this);

            if (lid != null)
                StartCoroutine(OpenLid());
        }

        private IEnumerator OpenLid()
        {
            var closed = lid.localRotation;
            var open = closed * Quaternion.Euler(openAngle, 0f, 0f);
            for (var elapsed = 0f; elapsed < openDuration; elapsed += Time.deltaTime)
            {
                lid.localRotation = Quaternion.Slerp(closed, open, elapsed / openDuration);
                yield return null;
            }
            lid.localRotation = open;
        }
    }
}
