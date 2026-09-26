using System;
using System.Collections;
using BattleArena.Core;
using UnityEngine;

namespace BattleArena.Interaction
{
    /// <summary>Restores a share of the player's health, then recharges.</summary>
    public sealed class HealingShrine : MonoBehaviour, IInteractable
    {
        [SerializeField, Range(0.05f, 1f)] private float healFraction = 0.5f;
        [SerializeField, Min(0f)] private float rechargeTime = 60f;
        [SerializeField] private Renderer crystal;
        [SerializeField] private Material readyMaterial;
        [SerializeField] private Material depletedMaterial;

        private bool _isReady = true;

        public string Prompt => "Heal";
        public bool CanInteract => _isReady;
        public Vector3 Position => transform.position;

        public event Action<IInteractable> StateChanged;

        // Shrines are permanent scene objects.
        event Action<IInteractable> IInteractable.Removed { add { } remove { } }

        public void Interact(PlayerInteractor interactor)
        {
            if (!_isReady)
                return;

            var health = interactor.Health;
            health.Heal(health.Max * healFraction);
            EventBus<NotificationEvent>.Raise(new NotificationEvent("The shrine restores your health"));
            StartCoroutine(Recharge());
        }

        private IEnumerator Recharge()
        {
            SetReady(false);
            yield return WaitCache.Get(rechargeTime);
            SetReady(true);
        }

        private void SetReady(bool ready)
        {
            _isReady = ready;
            if (crystal != null)
                crystal.sharedMaterial = ready ? readyMaterial : depletedMaterial;
            StateChanged?.Invoke(this);
        }
    }
}
