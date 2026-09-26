using System.Collections;
using BattleArena.Core;
using BattleArena.Pooling;
using UnityEngine;

namespace BattleArena.Combat
{
    /// <summary>Pooled lingering area that damages and slows everything inside on a fixed tick.</summary>
    public sealed class DamageZone : MonoBehaviour, IPoolable
    {
        [Tooltip("Flat visual with a unit diameter, scaled to the zone radius.")]
        [SerializeField] private Transform area;
        [SerializeField] private ParticleSystem particles;

        public void Activate(float radius, float duration, float tickInterval, DamageProfile damagePerTick, int targetMask,
            float slowMultiplier)
        {
            var diameter = radius * 2f;
            area.localScale = new Vector3(diameter, area.localScale.y, diameter);

            if (particles != null)
            {
                var shape = particles.shape;
                shape.radius = radius;
                particles.Play();
            }

            StartCoroutine(Tick(radius, duration, tickInterval, damagePerTick, targetMask, slowMultiplier));
        }

        public void OnSpawned() { }

        public void OnDespawned()
        {
            StopAllCoroutines();
            if (particles != null)
                particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

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

            this.Despawn();
        }
    }
}
