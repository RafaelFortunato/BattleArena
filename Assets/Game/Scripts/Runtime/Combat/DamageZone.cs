using System.Collections;
using BattleArena.Core;
using BattleArena.Pooling;
using UnityEngine;

namespace BattleArena.Combat
{
    /// <summary>Pooled lingering area that damages and slows everything inside on a fixed tick.</summary>
    public sealed class DamageZone : MonoBehaviour, IPoolable
    {
        [Tooltip("Visual root authored for a radius of 1 (particles use hierarchy scaling); scaled to the zone radius.")]
        [SerializeField] private Transform area;
        [Tooltip("Seconds the visuals take to fade once the zone stops ticking.")]
        [SerializeField, Min(0f)] private float fadeOut = 0.8f;

        private ParticleSystem[] _particles;

        /// <summary>Caches the zone's particle systems.</summary>
        private void Awake() => _particles = area.GetComponentsInChildren<ParticleSystem>(true);

        /// <summary>
        /// Starts the zone: scales the visuals to <paramref name="radius"/> and damages and slows everything inside
        /// every <paramref name="tickInterval"/> seconds for <paramref name="duration"/> seconds.
        /// </summary>
        public void Activate(float radius, float duration, float tickInterval, DamageProfile damagePerTick, int targetMask,
            float slowMultiplier)
        {
            area.localScale = Vector3.one * radius;
            foreach (var particles in _particles)
                particles.Play(false);

            StartCoroutine(Tick(radius, duration, tickInterval, damagePerTick, targetMask, slowMultiplier));
        }

        /// <summary>Nothing to do: <see cref="Activate"/> starts the zone.</summary>
        public void OnSpawned() { }

        /// <summary>Stops ticking and clears the particles for the next use.</summary>
        public void OnDespawned()
        {
            StopAllCoroutines();
            foreach (var particles in _particles)
                particles.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        /// <summary>
        /// Applies damage and slow on every tick, then lets the visuals fade and returns the zone to its pool.
        /// </summary>
        private IEnumerator Tick(float radius, float duration, float tickInterval, DamageProfile damage, int targetMask,
            float slowMultiplier)
        {
            var wait = WaitCache.Get(tickInterval);
            var center = transform.position;
            // Slow lasts slightly longer than a tick so it doesn't flicker between ticks.
            var slowDuration = tickInterval * 1.5f;

            for (var elapsed = 0f; elapsed < duration; elapsed += tickInterval)
            {
                CombatQueries.DamageAndSlowInRadius(center, radius, damage, targetMask, slowMultiplier, slowDuration);
                yield return wait;
            }

            // Stop emitting and let the live particles (including the ground decal) finish their lifetime.
            foreach (var particles in _particles)
                particles.Stop(false, ParticleSystemStopBehavior.StopEmitting);
            yield return WaitCache.Get(fadeOut);
            this.Despawn();
        }
    }
}
