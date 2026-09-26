using UnityEngine;
using UnityEngine.Pool;

namespace BattleArena.Pooling
{
    /// <summary>Links an instance to the pool that owns it and forwards spawn/despawn callbacks.</summary>
    [DisallowMultipleComponent]
    public sealed class PooledObject : MonoBehaviour
    {
        private IPoolable[] _receivers;
        private IObjectPool<PooledObject> _pool;

        public bool IsSpawned { get; private set; }

        private void Awake() => _receivers = GetComponentsInChildren<IPoolable>(true);

        internal void Bind(IObjectPool<PooledObject> pool) => _pool = pool;

        internal void NotifySpawned()
        {
            IsSpawned = true;
            for (var i = 0; i < _receivers.Length; i++)
                _receivers[i].OnSpawned();
        }

        /// <summary>Returns the object to its pool. Safe to call more than once.</summary>
        public void Release()
        {
            if (!IsSpawned)
                return;

            IsSpawned = false;
            for (var i = 0; i < _receivers.Length; i++)
                _receivers[i].OnDespawned();

            if (_pool != null)
                _pool.Release(this);
            else
                Destroy(gameObject);
        }
    }

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
