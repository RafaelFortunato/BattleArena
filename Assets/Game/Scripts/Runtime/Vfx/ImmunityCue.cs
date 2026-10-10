using System.Collections;
using BattleArena.Combat;
using UnityEngine;

namespace BattleArena.Vfx
{
    /// <summary>
    /// Shows a shield bubble around the player while they are immune to damage (Phase Step), blinking just before it
    /// wears off. Only reacts to <see cref="Health.InvulnerabilityGranted"/>, so the brief grace period after every
    /// hit shows nothing.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public sealed class ImmunityCue : MonoBehaviour
    {
        [SerializeField] private GameObject bubble;
        [Tooltip("Seconds before the end at which the bubble starts blinking.")]
        [SerializeField, Min(0f)] private float warningTime = 0.5f;
        [SerializeField, Min(0.02f)] private float blinkInterval = 0.08f;

        private Health _health;
        private Coroutine _routine;

        /// <summary>Caches the health and hides the bubble.</summary>
        private void Awake()
        {
            _health = GetComponent<Health>();
            bubble.SetActive(false);
        }

        /// <summary>Listens for immunity grants.</summary>
        private void OnEnable() => _health.InvulnerabilityGranted += OnGranted;

        /// <summary>Stops listening and hides the bubble.</summary>
        private void OnDisable()
        {
            _health.InvulnerabilityGranted -= OnGranted;
            Stop();
        }

        /// <summary>Starts (or restarts) showing the bubble.</summary>
        private void OnGranted(float duration)
        {
            if (duration <= 0f)
                return;
            if (_routine != null)
                StopCoroutine(_routine);
            _routine = StartCoroutine(Play());
        }

        /// <summary>
        /// Shows the bubble until the immunity actually ends, so a second grant mid-way simply extends it, blinking it
        /// near the end.
        /// </summary>
        private IEnumerator Play()
        {
            var nextBlink = 0f;
            var visible = true;
            while (_health.IsInvulnerable)
            {
                if (_health.InvulnerableRemaining > warningTime)
                {
                    visible = true;
                }
                else if (Time.time >= nextBlink)
                {
                    visible = !visible;
                    nextBlink = Time.time + blinkInterval;
                }

                if (bubble.activeSelf != visible)
                    bubble.SetActive(visible);
                yield return null;
            }

            Stop();
        }

        /// <summary>Stops the routine and hides the bubble.</summary>
        private void Stop()
        {
            if (_routine != null)
            {
                StopCoroutine(_routine);
                _routine = null;
            }
            bubble.SetActive(false);
        }
    }
}
