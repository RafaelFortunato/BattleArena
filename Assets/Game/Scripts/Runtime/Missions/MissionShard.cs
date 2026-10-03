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

        public event Action<MissionShard> Collected;

        public void OnSpawned() => _collected = false;

        public void OnDespawned() => Collected = null;

        private void OnTriggerEnter(Collider other)
        {
            // The player's sensor volumes are children of the player root.
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
