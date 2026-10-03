using BattleArena.Pooling;
using UnityEngine;

namespace BattleArena.Audio
{
    /// <summary>
    /// Plays a sound where a pooled object appears: a projectile's launch, an explosion's boom. Hooked to the
    /// pool's spawn callback rather than OnEnable, so prewarming a pool stays silent and the sound is heard at the
    /// position the object was spawned at.
    /// </summary>
    public sealed class SpawnSound : MonoBehaviour, IPoolable
    {
        [SerializeField] private SoundEvent sound;

        public void OnSpawned() => AudioManager.PlayAt(sound, transform.position);

        public void OnDespawned() { }
    }
}
