using BattleArena.Audio;
using BattleArena.Pooling;
using UnityEngine;

namespace BattleArena.Pickups
{
    /// <summary>Experience dropped by enemies. Pulled in and collected by the player's <see cref="PickupMagnet"/>.</summary>
    public sealed class ExperienceGem : MonoBehaviour, IPoolable
    {
        [SerializeField] private SoundEvent collectSound;

        public int Value { get; private set; }
        public bool IsAttracted { get; private set; }

        public void Initialize(int value) => Value = value;

        public void Attract() => IsAttracted = true;

        public void Collect()
        {
            AudioManager.PlayAt(collectSound, transform.position);
            this.Despawn();
        }

        public void OnSpawned() => IsAttracted = false;

        public void OnDespawned() => IsAttracted = false;
    }
}
