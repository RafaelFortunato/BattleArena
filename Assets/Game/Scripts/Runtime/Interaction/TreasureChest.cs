using System;
using System.Collections;
using BattleArena.Audio;
using BattleArena.Core;
using BattleArena.Pooling;
using UnityEngine;

namespace BattleArena.Interaction
{
    /// <summary>
    /// Pooled single-use chest spawned by <see cref="ChestSpawner"/>. Once its lid is open it rewards a choice of
    /// upgrade cards (raised as <see cref="TreasureOpenedEvent"/>), then shrinks away and returns to its pool.
    /// </summary>
    public sealed class TreasureChest : MonoBehaviour, IInteractable, IPoolable
    {
        [SerializeField] private Transform lid;
        [SerializeField] private float openAngle = -110f;
        [SerializeField, Min(0.01f)] private float openDuration = 0.4f;
        [SerializeField, Min(0f)] private float lingerAfterOpen = 0.9f;
        [SerializeField, Min(0.01f)] private float appearDuration = 0.35f;
        [SerializeField, Min(0.01f)] private float vanishDuration = 0.35f;
        [SerializeField] private TimedRelease openEffect;
        [SerializeField] private SoundEvent openSound;

        private Quaternion _closedLidRotation;
        private bool _isOpen;

        public string Prompt => "Open chest";
        public bool CanInteract => !_isOpen;
        public Vector3 Position => transform.position;

        public event Action<IInteractable> StateChanged;
        public event Action<IInteractable> Removed;

        private void Awake() => _closedLidRotation = lid.localRotation;

        public void OnSpawned()
        {
            _isOpen = false;
            lid.localRotation = _closedLidRotation;
            StartCoroutine(ScaleTo(Vector3.zero, Vector3.one, appearDuration, overshoot: true));
        }

        public void OnDespawned()
        {
            StopAllCoroutines();
            transform.localScale = Vector3.one;
        }

        public void Interact(PlayerInteractor interactor)
        {
            if (_isOpen)
                return;

            _isOpen = true;
            StateChanged?.Invoke(this);

            if (openEffect != null)
                PoolManager.Spawn(openEffect, transform.position + Vector3.up, Quaternion.identity);
            AudioManager.PlayAt(openSound, transform.position);

            StartCoroutine(OpenAndVanish());
        }

        private IEnumerator OpenAndVanish()
        {
            var open = _closedLidRotation * Quaternion.Euler(openAngle, 0f, 0f);
            for (var elapsed = 0f; elapsed < openDuration; elapsed += Time.deltaTime)
            {
                lid.localRotation = Quaternion.Slerp(_closedLidRotation, open, elapsed / openDuration);
                yield return null;
            }
            lid.localRotation = open;

            // The card choice freezes time, so it waits until the lid is open; the chest vanishes after it closes.
            EventBus<TreasureOpenedEvent>.Raise(default);

            yield return WaitCache.Get(lingerAfterOpen);
            yield return ScaleTo(Vector3.one, Vector3.zero, vanishDuration, overshoot: false);

            Removed?.Invoke(this);
            this.Despawn();
        }

        private IEnumerator ScaleTo(Vector3 from, Vector3 to, float duration, bool overshoot)
        {
            for (var elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
            {
                var t = elapsed / duration;
                // Back-out easing gives a small "pop" when appearing.
                var eased = overshoot ? 1f + 2.7f * Mathf.Pow(t - 1f, 3f) + 1.7f * Mathf.Pow(t - 1f, 2f) : t * t;
                transform.localScale = Vector3.LerpUnclamped(from, to, eased);
                yield return null;
            }
            transform.localScale = to;
        }
    }
}
