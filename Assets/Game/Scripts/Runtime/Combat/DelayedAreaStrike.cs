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
        [Tooltip("Seconds the falling object rests on the ground, pulsing, before it goes off (a bomb's fuse). " +
                 "The marker keeps filling until then. Zero goes off on landing.")]
        [SerializeField, Min(0f)] private float fuseAfterLanding;

        /// <summary>Strike at this position after <paramref name="delay"/>; the falling object drops from above.</summary>
        public void Arm(float radius, float delay, DamageProfile damage, int targetMask) =>
            StartCoroutine(Run(radius, delay, damage, targetMask, lobbed: false, Vector3.zero));

        /// <summary>Like <see cref="Arm"/>, but the falling object flies in an arc from <paramref name="launchPosition"/>.</summary>
        public void ArmLobbed(Vector3 launchPosition, float radius, float flightTime, DamageProfile damage, int targetMask) =>
            StartCoroutine(Run(radius, flightTime, damage, targetMask, lobbed: true, launchPosition));

        /// <summary>Nothing to do: <see cref="Arm"/> or <see cref="ArmLobbed"/> starts the strike.</summary>
        public void OnSpawned() { }

        /// <summary>Cancels a strike still in progress.</summary>
        public void OnDespawned() => StopAllCoroutines();

        /// <summary>
        /// Animates the telegraph (filling marker, falling or lobbed object, fuse pulse), then deals the damage, spawns
        /// the impact effect and despawns.
        /// </summary>
        private IEnumerator Run(float radius, float delay, DamageProfile damage, int targetMask, bool lobbed, Vector3 launchPosition)
        {
            var diameter = radius * 2f;
            var origin = transform.position;
            outline.localScale = new Vector3(diameter, outline.localScale.y, diameter);
            var restingScale = Vector3.one;
            if (fallingObject != null)
            {
                fallingObject.gameObject.SetActive(true);
                restingScale = fallingObject.localScale;
            }

            // Visual-only animation for the lifetime of the telegraph: the flight, then any fuse on the ground.
            var total = delay + fuseAfterLanding;
            var pulsePhase = 0f;
            for (var elapsed = 0f; elapsed < total; elapsed += Time.deltaTime)
            {
                var size = diameter * (elapsed / total);
                fill.localScale = new Vector3(size, fill.localScale.y, size);
                if (fallingObject == null)
                {
                    yield return null;
                    continue;
                }

                if (elapsed < delay)
                {
                    var t = elapsed / delay;
                    fallingObject.position = lobbed
                        ? Vector3.Lerp(launchPosition, origin, t) + Vector3.up * (arcHeight * 4f * t * (1f - t))
                        : origin + Vector3.up * (fallHeight * (1f - t * t));
                }
                else
                {
                    // Landed: sits on the spot and throbs faster and faster as the fuse burns down.
                    fallingObject.position = origin;
                    var burnt = (elapsed - delay) / fuseAfterLanding;
                    pulsePhase += Time.deltaTime * Mathf.Lerp(4f, 14f, burnt) * Mathf.PI * 2f;
                    fallingObject.localScale = restingScale * (1f + 0.15f * Mathf.Abs(Mathf.Sin(pulsePhase)));
                }
                yield return null;
            }

            if (fallingObject != null)
            {
                fallingObject.localScale = restingScale;
                fallingObject.gameObject.SetActive(false);
            }

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
