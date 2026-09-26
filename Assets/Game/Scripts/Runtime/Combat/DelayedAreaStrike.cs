using System.Collections;
using BattleArena.Pooling;
using UnityEngine;

namespace BattleArena.Combat
{
    /// <summary>
    /// Telegraphed area attack: shows a ground marker that fills up, then deals damage in a radius.
    /// Used by Meteor Shower and the boss slam.
    /// </summary>
    public sealed class DelayedAreaStrike : MonoBehaviour, IPoolable
    {
        [Tooltip("Flat marker scaled to the strike radius (unit diameter mesh).")]
        [SerializeField] private Transform outline;
        [Tooltip("Flat marker that grows from zero to the strike radius during the delay.")]
        [SerializeField] private Transform fill;
        [Tooltip("Optional object that falls from above and lands when the strike hits (e.g. a meteor).")]
        [SerializeField] private Transform fallingObject;
        [SerializeField, Min(0f)] private float fallHeight = 12f;
        [SerializeField] private TimedRelease impactEffect;

        public void Arm(float radius, float delay, DamageProfile damage, int targetMask) =>
            StartCoroutine(Run(radius, delay, damage, targetMask));

        public void OnSpawned() { }

        public void OnDespawned() => StopAllCoroutines();

        private IEnumerator Run(float radius, float delay, DamageProfile damage, int targetMask)
        {
            var diameter = radius * 2f;
            var origin = transform.position;
            outline.localScale = new Vector3(diameter, outline.localScale.y, diameter);
            if (fallingObject != null)
                fallingObject.gameObject.SetActive(true);

            // Visual-only animation for the lifetime of the telegraph.
            for (var elapsed = 0f; elapsed < delay; elapsed += Time.deltaTime)
            {
                var t = elapsed / delay;
                var size = diameter * t;
                fill.localScale = new Vector3(size, fill.localScale.y, size);
                if (fallingObject != null)
                    fallingObject.position = origin + Vector3.up * (fallHeight * (1f - t * t));
                yield return null;
            }

            if (fallingObject != null)
                fallingObject.gameObject.SetActive(false);

            CombatQueries.DamageInRadius(origin, radius, damage, targetMask);

            if (impactEffect != null)
            {
                var effect = PoolManager.Spawn(impactEffect, origin, Quaternion.identity);
                effect.transform.localScale = Vector3.one * radius;
            }

            this.Despawn();
        }
    }
}
