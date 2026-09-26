using BattleArena.Pooling;
using UnityEngine;

namespace BattleArena.Combat
{
    /// <summary>
    /// Pooled physics projectile. Movement is driven by the Rigidbody velocity set once at launch,
    /// so there is no per-frame script cost. Hits are filtered by the layer collision matrix.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class Projectile : MonoBehaviour, IPoolable
    {
        [SerializeField, Min(0.1f)] private float lifetime = 3f;
        [SerializeField] private TimedRelease impactEffect;

        private Rigidbody _body;
        private DamageProfile _damage;
        private int _pierceRemaining;
        private float _explosionRadius;
        private int _explosionMask;
        private bool _inFlight;

        private void Awake()
        {
            _body = GetComponent<Rigidbody>();
            _body.useGravity = false;
        }

        /// <param name="pierce">How many extra targets the projectile passes through.</param>
        /// <param name="explosionRadius">When above zero, the projectile explodes on the first hit and damages everything in the radius.</param>
        public void Launch(Vector3 direction, float speed, DamageProfile damage, int pierce = 0, float explosionRadius = 0f, int explosionMask = 0)
        {
            _damage = damage;
            _pierceRemaining = pierce;
            _explosionRadius = explosionRadius;
            _explosionMask = explosionMask;
            _inFlight = true;

            transform.rotation = Quaternion.LookRotation(direction);
            _body.linearVelocity = direction * speed;
            // Invoke avoids allocating a coroutine for every shot.
            Invoke(nameof(Expire), lifetime);
        }

        public void OnSpawned() { }

        public void OnDespawned()
        {
            _inFlight = false;
            _body.linearVelocity = Vector3.zero;
            CancelInvoke();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!_inFlight)
                return;

            if (_explosionRadius > 0f)
            {
                Explode();
                return;
            }

            if (!other.TryGetComponent(out IDamageable target))
            {
                // Solid obstacle.
                Impact();
                return;
            }

            if (!target.IsAlive)
                return;

            _damage.ApplyTo(target);
            if (_pierceRemaining-- <= 0)
                Impact();
            else
                SpawnImpactEffect();
        }

        private void Expire()
        {
            if (_explosionRadius > 0f)
                Explode();
            else
                this.Despawn();
        }

        private void Explode()
        {
            CombatQueries.DamageInRadius(transform.position, _explosionRadius, _damage, _explosionMask);
            Impact();
        }

        private void Impact()
        {
            _inFlight = false;
            SpawnImpactEffect();
            this.Despawn();
        }

        private void SpawnImpactEffect()
        {
            if (impactEffect != null)
                PoolManager.Spawn(impactEffect, transform.position, Quaternion.identity);
        }
    }
}
