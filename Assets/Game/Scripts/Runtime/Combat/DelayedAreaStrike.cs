using System.Collections;
using BattleArena.Pooling;
using BattleArena.Vfx;
using UnityEngine;

namespace BattleArena.Combat
{
    /// <summary>
    /// Telegraphed area attack: shows a ground marker that fills up, then deals damage in a radius.
    /// Used by Meteor Shower, the boss slam, enemy curses, bombs and self-destructs.
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
        [Tooltip("Peak height of the arc when the strike is lobbed from a launch point.")]
        [SerializeField, Min(0f)] private float arcHeight = 4f;
        [SerializeField] private TimedRelease impactEffect;
        [Tooltip("Screen shake when it lands, weaker the further from the player. Zero for none.")]
        [SerializeField, Range(0f, 1f)] private float impactShake;

        /// <summary>Strike at this position after <paramref name="delay"/>; the falling object drops from above.</summary>
        public void Arm(float radius, float delay, DamageProfile damage, int targetMask) =>
            StartCoroutine(Run(radius, delay, damage, targetMask, lobbed: false, Vector3.zero));

        /// <summary>Like <see cref="Arm"/>, but the falling object flies in an arc from <paramref name="launchPosition"/>.</summary>
        public void ArmLobbed(Vector3 launchPosition, float radius, float flightTime, DamageProfile damage, int targetMask) =>
            StartCoroutine(Run(radius, flightTime, damage, targetMask, lobbed: true, launchPosition));

        public void OnSpawned() { }

        public void OnDespawned() => StopAllCoroutines();

        private IEnumerator Run(float radius, float delay, DamageProfile damage, int targetMask, bool lobbed, Vector3 launchPosition)
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
                {
                    fallingObject.position = lobbed
                        ? Vector3.Lerp(launchPosition, origin, t) + Vector3.up * (arcHeight * 4f * t * (1f - t))
                        : origin + Vector3.up * (fallHeight * (1f - t * t));
                }
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

            if (impactShake > 0f)
                ScreenShake.AddAt(origin, impactShake);

            this.Despawn();
        }
    }
}
