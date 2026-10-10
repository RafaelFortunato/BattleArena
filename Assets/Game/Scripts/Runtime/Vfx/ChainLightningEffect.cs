using System.Collections;
using System.Collections.Generic;
using BattleArena.Combat;
using BattleArena.Pooling;
using UnityEngine;

namespace BattleArena.Vfx
{
    /// <summary>
    /// Pooled chain lightning. The bolt travels one jump at a time so the chain can be followed by eye: each link
    /// appears after a short delay, deals its damage when it lands, marks the enemy with a shock effect and
    /// lingers, flickering, while following the enemies it connects.
    /// </summary>
    public sealed class ChainLightningEffect : MonoBehaviour, IPoolable
    {
        /// <summary>
        /// Most links one chain can have: 6 targets + 10 pierce (its cap) + 6 from three Arc Jump upgrades, with a
        /// spare.
        /// </summary>
        public const int MaxLinks = 24;
        private const int MaxSegments = 24;

        [Tooltip("One wide glow line per link.")]
        [SerializeField] private LineRenderer[] glows;
        [Tooltip("Two thin core strands per link (link i uses 2i and 2i + 1).")]
        [SerializeField] private LineRenderer[] cores;
        [SerializeField, Min(0f)] private float jumpDelay = 0.1f;
        [SerializeField, Min(0.05f)] private float linkLifetime = 0.55f;
        [SerializeField, Min(0.01f)] private float flickerInterval = 0.05f;
        [SerializeField, Min(0f)] private float jitter = 0.35f;
        [SerializeField, Min(0.1f)] private float segmentLength = 0.55f;
        [SerializeField, Min(0f)] private float glowWidth = 1.1f;
        [SerializeField, Min(0f)] private float coreWidth = 0.17f;
        [SerializeField] private TimedRelease hitEffect;
        [SerializeField] private FollowTarget shockEffect;

        private readonly Collider[] _targets = new Collider[MaxLinks];
        private readonly Vector3[] _points = new Vector3[MaxLinks + 1];
        private readonly bool[] _frozen = new bool[MaxLinks];
        private readonly float[] _born = new float[MaxLinks];
        private readonly Vector3[,] _offsets = new Vector3[MaxLinks * 2, MaxSegments + 1];
        private readonly Vector3[] _buffer = new Vector3[MaxSegments + 1];
        private Transform _caster;
        private Vector3 _casterOffset;
        private int _count;
        private int _shown;
        private float _nextFlicker;

        /// <summary>Starts the chain from the caster through <paramref name="count"/> targets.</summary>
        /// <param name="caster">The caster; the first link stays attached to it while it moves.</param>
        /// <param name="origin">Where the bolt leaves the caster (the staff's crystal).</param>
        /// <param name="targets">The chain's targets, in jump order.</param>
        /// <param name="count">How many of <paramref name="targets"/> to use.</param>
        /// <param name="damage">Damage of the first strike.</param>
        /// <param name="damageFalloff">Damage multiplier applied per jump.</param>
        public void Play(Transform caster, Vector3 origin, IReadOnlyList<Collider> targets, int count, DamageProfile damage, float damageFalloff)
        {
            _caster = caster;
            _casterOffset = origin - caster.position;
            _count = Mathf.Min(count, MaxLinks);
            for (var i = 0; i < _count; i++)
            {
                _targets[i] = targets[i];
                _frozen[i] = false;
                _points[i + 1] = StrikePoint(targets[i]);
            }
            _points[0] = origin;
            StartCoroutine(Run(damage, damageFalloff));
        }

        /// <summary>Starts with no links shown.</summary>
        public void OnSpawned()
        {
            _shown = 0;
            _nextFlicker = 0f;
            SetLinksVisible(0);
        }

        /// <summary>Stops the chain and forgets its targets.</summary>
        public void OnDespawned()
        {
            StopAllCoroutines();
            _caster = null;
            for (var i = 0; i < _count; i++)
                _targets[i] = null;
            _count = 0;
            _shown = 0;
            SetLinksVisible(0);
        }

        /// <summary>
        /// Reveals one link per jump, striking its target with damage reduced by the falloff each jump, then despawns
        /// once the last link fades.
        /// </summary>
        private IEnumerator Run(DamageProfile damage, float falloff)
        {
            for (var i = 0; i < _count; i++)
            {
                if (i > 0)
                    for (var wait = 0f; wait < jumpDelay; wait += Time.deltaTime)
                        yield return null;

                RefreshPoints();
                _born[i] = Time.time;
                _shown = i + 1;
                Reshape(i);
                SetLinksVisible(_shown);
                Strike(i, damage.Scaled(Mathf.Pow(falloff, i)));
            }

            for (var wait = 0f; wait < linkLifetime; wait += Time.deltaTime)
                yield return null;
            this.Despawn();
        }

        /// <summary>Damages the link's target if still alive, and spawns the hit and shock effects there.</summary>
        private void Strike(int index, DamageProfile damage)
        {
            var target = _targets[index];
            var point = _points[index + 1];
            if (target != null && target.gameObject.activeInHierarchy && target.TryGetComponent(out IDamageable damageable) && damageable.IsAlive)
                damage.ApplyTo(damageable);

            if (hitEffect != null)
                PoolManager.Spawn(hitEffect, point, Quaternion.identity);
            if (shockEffect != null && target != null)
            {
                var shock = PoolManager.Spawn(shockEffect, point, Quaternion.identity);
                shock.Follow(target.transform, point - target.transform.position);
            }
        }

        /// <summary>
        /// Keeps the links on their moving endpoints, re-jitters them on the flicker interval and fades each one out.
        /// </summary>
        private void LateUpdate()
        {
            if (_shown == 0)
                return;

            RefreshPoints();
            if (Time.time >= _nextFlicker)
            {
                _nextFlicker = Time.time + flickerInterval;
                for (var i = 0; i < _shown; i++)
                    Reshape(i);
            }

            for (var i = 0; i < _shown; i++)
            {
                var life = 1f - (Time.time - _born[i]) / linkLifetime;
                if (life <= 0f)
                {
                    glows[i].enabled = cores[i * 2].enabled = cores[i * 2 + 1].enabled = false;
                    continue;
                }

                // Flash in brightly, then thin out; the flicker keeps the width lively.
                var pulse = 0.75f + 0.25f * Mathf.PerlinNoise(Time.time * 30f, i);
                var width = Mathf.Min(1f, life * 1.6f) * pulse;
                glows[i].widthMultiplier = glowWidth * width;
                cores[i * 2].widthMultiplier = coreWidth * width;
                cores[i * 2 + 1].widthMultiplier = coreWidth * 0.6f * width;
                Draw(glows[i], i, i * 2);
                Draw(cores[i * 2], i, i * 2);
                Draw(cores[i * 2 + 1], i, i * 2 + 1);
            }
        }

        /// <summary>
        /// Moves the chain's points with the caster and with targets that are still alive; a dead target's point stays
        /// where it fell.
        /// </summary>
        private void RefreshPoints()
        {
            if (_caster != null)
                _points[0] = _caster.position + _casterOffset;
            for (var i = 0; i < _count; i++)
            {
                if (_frozen[i])
                    continue;
                var target = _targets[i];
                // Killed enemies are pooled and may respawn elsewhere: pin the arc where they fell.
                if (target == null || !target.gameObject.activeInHierarchy)
                    _frozen[i] = true;
                else
                    _points[i + 1] = StrikePoint(target);
            }
        }

        /// <summary>Upper body rather than the centre, so arcs pass over the crowd instead of through the bodies.</summary>
        private static Vector3 StrikePoint(Collider target)
        {
            var bounds = target.bounds;
            return new Vector3(bounds.center.x, bounds.max.y - bounds.extents.y * 0.3f, bounds.center.z);
        }

        /// <summary>New random zig-zag for both strands of a link.</summary>
        private void Reshape(int link)
        {
            for (var strand = 0; strand < 2; strand++)
            for (var s = 0; s <= MaxSegments; s++)
                _offsets[link * 2 + strand, s] = Random.insideUnitSphere * (strand == 0 ? 1f : 0.7f);
        }

        /// <summary>Writes one strand of a link into its line renderer.</summary>
        private void Draw(LineRenderer line, int link, int strand)
        {
            var from = _points[link];
            var to = _points[link + 1];
            var segments = Mathf.Clamp(Mathf.CeilToInt(Vector3.Distance(from, to) / segmentLength), 3, MaxSegments);
            for (var s = 0; s <= segments; s++)
            {
                var t = (float)s / segments;
                // No jitter at the ends so the bolt meets each enemy exactly.
                var envelope = Mathf.Sin(t * Mathf.PI);
                _buffer[s] = Vector3.Lerp(from, to, t) + _offsets[strand, s] * (jitter * envelope);
            }
            line.positionCount = segments + 1;
            line.SetPositions(_buffer);
        }

        /// <summary>Shows the first <paramref name="count"/> links and hides the rest.</summary>
        private void SetLinksVisible(int count)
        {
            for (var i = 0; i < glows.Length; i++)
            {
                var visible = i < count;
                glows[i].enabled = visible;
                cores[i * 2].enabled = visible;
                cores[i * 2 + 1].enabled = visible;
            }
        }
    }
}
