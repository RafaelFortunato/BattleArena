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
        private float _groundHeight;
        private bool _inFlight;

        private void Awake()
        {
            _body = GetComponent<Rigidbody>();
            _body.useGravity = false;
        }

        /// <param name="pierce">How many extra targets the projectile passes through.</param>
        /// <param name="explosionRadius">
        /// When above zero, the projectile explodes on every enemy it reaches, damaging everything in the radius, and
        /// flies on while it has pierce left.
        /// </param>
        /// <param name="groundHeight">
        /// Floor height under a projectile flying level above it. Explosions and their effect go off there rather
        /// than in mid-air, so the blast and its scorch sit on the ground. Unset, they go off where the projectile is.
        /// </param>
        public void Launch(Vector3 direction, float speed, DamageProfile damage, int pierce = 0, float explosionRadius = 0f,
            int explosionMask = 0, float groundHeight = float.NegativeInfinity)
        {
            _damage = damage;
            _pierceRemaining = pierce;
            _explosionRadius = explosionRadius;
            _explosionMask = explosionMask;
            _groundHeight = groundHeight;
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

            if (!other.TryGetComponent(out IDamageable target))
            {
                // Solid obstacle: an explosive goes off against it, anything else just stops.
                if (_explosionRadius > 0f)
                    Explode();
                else
                    Impact();
                return;
            }

            if (!target.IsAlive)
                return;

            if (_explosionRadius > 0f)
                CombatQueries.DamageInRadius(EffectPosition, _explosionRadius, _damage, _explosionMask);
            else
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
            CombatQueries.DamageInRadius(EffectPosition, _explosionRadius, _damage, _explosionMask);
            Impact();
        }

        /// <summary>Where blasts and impact effects go off: on the floor under a level flyer, else where the projectile is.</summary>
        private Vector3 EffectPosition
        {
            get
            {
                var position = transform.position;
                if (_explosionRadius > 0f && _groundHeight > float.NegativeInfinity)
                    position.y = _groundHeight;
                return position;
            }
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
                PoolManager.Spawn(impactEffect, EffectPosition, Quaternion.identity);
        }
    }
}
