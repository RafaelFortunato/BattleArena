namespace BattleArena.Pooling
{
    /// <summary>Receives lifecycle callbacks from <see cref="PooledObject"/> instead of relying on Awake/OnDestroy.</summary>
    public interface IPoolable
    {
        /// <summary>Called after the object is positioned and activated.</summary>
        void OnSpawned();

        /// <summary>Called right before the object is deactivated and returned to its pool.</summary>
        void OnDespawned();
    }
}
