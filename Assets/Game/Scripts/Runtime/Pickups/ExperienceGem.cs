using BattleArena.Pooling;
using UnityEngine;

namespace BattleArena.Pickups
{
    /// <summary>Experience dropped by enemies. Pulled in and collected by the player's <see cref="PickupMagnet"/>.</summary>
    public sealed class ExperienceGem : MonoBehaviour, IPoolable
    {
        public int Value { get; private set; }
        public bool IsAttracted { get; private set; }

        public void Initialize(int value) => Value = value;

        public void Attract() => IsAttracted = true;

        public void Collect() => this.Despawn();

        public void OnSpawned() => IsAttracted = false;

        public void OnDespawned() => IsAttracted = false;
    }
}
