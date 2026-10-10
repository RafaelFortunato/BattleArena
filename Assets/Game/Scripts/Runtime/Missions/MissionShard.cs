using System;
using BattleArena.Audio;
using BattleArena.Player;
using BattleArena.Pooling;
using UnityEngine;

namespace BattleArena.Missions
{
    /// <summary>Pooled collectible for the shard mission. Picked up when the player's sensors touch it.</summary>
    [RequireComponent(typeof(Collider))]
    public sealed class MissionShard : MonoBehaviour, IPoolable
    {
        [SerializeField] private TimedRelease collectEffect;
        [SerializeField] private SoundEvent collectSound;

        private bool _collected;

        /// <summary>Raised once when the player picks the shard up.</summary>
        public event Action<MissionShard> Collected;

        /// <summary>Makes the shard collectible again.</summary>
        public void OnSpawned() => _collected = false;

        /// <summary>Drops the collection listener.</summary>
        public void OnDespawned() => Collected = null;

        /// <summary>
        /// Collects the shard when one of the player's sensor volumes (children of the player root) touches it.
        /// </summary>
        private void OnTriggerEnter(Collider other)
        {
            if (_collected || other.GetComponentInParent<PlayerController>() == null)
                return;

            _collected = true;
            if (collectEffect != null)
                PoolManager.Spawn(collectEffect, transform.position, Quaternion.identity);
            AudioManager.PlayAt(collectSound, transform.position);
            Collected?.Invoke(this);
            this.Despawn();
        }
    }
}
