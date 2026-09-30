using System;
using System.Collections;
using BattleArena.Core;
using BattleArena.Pooling;
using UnityEngine;

namespace BattleArena.Interaction
{
    /// <summary>
    /// A well full of healing water. Drinking restores a share of the player's health and drains the well;
    /// it stays empty while recharging and refills just before it can be used again.
    /// </summary>
    public sealed class HealingWell : MonoBehaviour, IInteractable
    {
        [SerializeField, Range(0.05f, 1f)] private float healFraction = 0.5f;
        [SerializeField, Min(0f)] private float rechargeTime = 60f;

        [Header("Water")]
        [SerializeField] private Transform water;
        [Tooltip("How far the water surface sinks when drained, until it is hidden below the shaft floor.")]
        [SerializeField, Min(0f)] private float drainDepth = 0.65f;
        [SerializeField, Min(0.05f)] private float drainDuration = 0.9f;
        [Tooltip("The water rises back over the last seconds of the recharge.")]
        [SerializeField, Min(0.05f)] private float refillDuration = 2.5f;
        [SerializeField] private TimedRelease useEffect;

        private bool _isReady = true;
        private Vector3 _waterFullPosition;

        public string Prompt => "Heal";
        public bool CanInteract => _isReady;
        public Vector3 Position => transform.position;

        public event Action<IInteractable> StateChanged;

        // Wells are permanent scene objects.
        event Action<IInteractable> IInteractable.Removed { add { } remove { } }

        private void Awake()
        {
            if (water != null)
                _waterFullPosition = water.localPosition;
        }

        public void Interact(PlayerInteractor interactor)
        {
            if (!_isReady)
                return;

            var health = interactor.Health;
            health.Heal(health.Max * healFraction);
            EventBus<NotificationEvent>.Raise(new NotificationEvent("The well's water restores your health"));
            if (useEffect != null)
                PoolManager.Spawn(useEffect, water != null ? water.position : transform.position + Vector3.up, Quaternion.identity);
            StartCoroutine(Recharge());
        }

        private IEnumerator Recharge()
        {
            SetReady(false);
            yield return MoveWater(0f, 1f, drainDuration);
            SetWaterVisible(false);

            yield return WaitCache.Get(Mathf.Max(0f, rechargeTime - drainDuration - refillDuration));

            SetWaterVisible(true);
            yield return MoveWater(1f, 0f, refillDuration);
            SetReady(true);
        }

        /// <summary>Animates the water from one drain level (0 full, 1 empty) to another.</summary>
        private IEnumerator MoveWater(float from, float to, float duration)
        {
            if (water == null)
                yield break;

            for (var time = 0f; time < duration; time += Time.deltaTime)
            {
                SetDrain(Mathf.SmoothStep(from, to, time / duration));
                yield return null;
            }
            SetDrain(to);
        }

        private void SetDrain(float drain) => water.localPosition = _waterFullPosition + Vector3.down * (drainDepth * drain);

        private void SetWaterVisible(bool visible)
        {
            if (water != null)
                water.gameObject.SetActive(visible);
        }

        private void SetReady(bool ready)
        {
            _isReady = ready;
            StateChanged?.Invoke(this);
        }
    }
}
