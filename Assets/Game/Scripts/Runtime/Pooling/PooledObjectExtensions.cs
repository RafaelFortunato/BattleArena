using UnityEngine;

namespace BattleArena.Pooling
{
    /// <summary>Pool helpers callable on any component.</summary>
    public static class PooledObjectExtensions
    {
        /// <summary>Releases the component's GameObject to its pool, or destroys it when it isn't pooled.</summary>
        public static void Despawn(this Component component)
        {
            if (component.TryGetComponent(out PooledObject pooled))
                pooled.Release();
            else
                Object.Destroy(component.gameObject);
        }
    }
}
