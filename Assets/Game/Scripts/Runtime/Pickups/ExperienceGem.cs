using BattleArena.Audio;
using BattleArena.Pooling;
using UnityEngine;

namespace BattleArena.Pickups
{
    /// <summary>Experience dropped by enemies. Pulled in and collected by the player's <see cref="PickupMagnet"/>.</summary>
    public sealed class ExperienceGem : MonoBehaviour, IPoolable
    {
        [SerializeField] private SoundEvent collectSound;
        [Tooltip("Small sparkle where the gem is picked up. Optional.")]
        [SerializeField] private TimedRelease collectEffect;
        [Tooltip("Pickups closer together than this (seconds) build a chain; each link plays a semitone higher.")]
        [SerializeField, Min(0f)] private float chainWindow = 0.6f;
        [Tooltip("Highest the chain climbs, in semitones above the base pitch.")]
        [SerializeField, Range(0, 24)] private int maxChainSemitones = 12;

        // Shared by every gem: the chain belongs to the player's pickups, not to any one gem.
        private static float _lastCollectAt = float.NegativeInfinity;
        private static int _chain;

        /// <summary>Experience the gem is worth.</summary>
        public int Value { get; private set; }
        /// <summary>True once a magnet is pulling the gem in.</summary>
        public bool IsAttracted { get; private set; }

        /// <summary>Sets how much experience the gem is worth.</summary>
        public void Initialize(int value) => Value = value;

        /// <summary>Marks the gem as being pulled in, so no other magnet takes it.</summary>
        public void Attract() => IsAttracted = true;

        /// <summary>
        /// Plays the pickup sound, a semitone higher for each gem in a quick chain so a big haul climbs a scale, then
        /// despawns.
        /// </summary>
        public void Collect()
        {
            var now = Time.time;
            _chain = now - _lastCollectAt <= chainWindow ? Mathf.Min(_chain + 1, maxChainSemitones) : 0;
            _lastCollectAt = now;

            AudioManager.PlayAt(collectSound, transform.position, Mathf.Pow(2f, _chain / 12f));
            if (collectEffect != null)
                PoolManager.Spawn(collectEffect, transform.position + Vector3.up * 0.6f, Quaternion.identity);
            this.Despawn();
        }

        /// <summary>A fresh gem is free to be attracted.</summary>
        public void OnSpawned() => IsAttracted = false;

        /// <summary>Releases the gem from any magnet.</summary>
        public void OnDespawned() => IsAttracted = false;
    }
}
