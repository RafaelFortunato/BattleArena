using System.Collections;
using BattleArena.Pooling;
using BattleArena.Vfx;
using UnityEngine;

namespace BattleArena.Combat
{
    /// <summary>
    /// Pooled ring of damaging orbs that follows a target for a limited time. The prefab holds the
    /// maximum number of orbs; only the requested count is enabled. Rotating this root is the only per-frame work.
    /// </summary>
    public sealed class OrbitingOrbs : MonoBehaviour, IPoolable
    {
        [SerializeField] private ContactDamage[] orbs;
        [SerializeField, Min(0f)] private float heightOffset = 1f;

        public void Activate(Transform follow, DamageProfile damage, int count, float radius, float duration, float degreesPerSecond)
        {
            count = Mathf.Clamp(count, 1, orbs.Length);
            for (var i = 0; i < orbs.Length; i++)
            {
                var active = i < count;
                orbs[i].gameObject.SetActive(active);
                if (!active)
                    continue;

                var angle = 360f / count * i * Mathf.Deg2Rad;
                orbs[i].transform.localPosition = new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * radius;
                orbs[i].Damage = damage;
            }

            // Place the ring before the first frame so the orb trails don't streak in from the pool position.
            transform.SetPositionAndRotation(follow.position + Vector3.up * heightOffset, Quaternion.identity);
            if (TryGetComponent(out VfxRestart vfx))
                vfx.ClearTrails();

            StartCoroutine(Orbit(follow, duration, degreesPerSecond));
        }

        public void OnSpawned() { }

        public void OnDespawned() => StopAllCoroutines();

        private IEnumerator Orbit(Transform follow, float duration, float degreesPerSecond)
        {
            var angle = 0f;
            for (var elapsed = 0f; elapsed < duration && follow != null; elapsed += Time.deltaTime)
            {
                angle += degreesPerSecond * Time.deltaTime;
                transform.SetPositionAndRotation(follow.position + Vector3.up * heightOffset, Quaternion.Euler(0f, angle, 0f));
                yield return null;
            }

            this.Despawn();
        }
    }
}
