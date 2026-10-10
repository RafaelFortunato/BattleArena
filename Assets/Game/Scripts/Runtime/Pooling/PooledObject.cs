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

        /// <summary>True while the instance is out of its pool.</summary>
        public bool IsSpawned { get; private set; }

        /// <summary>Collects every <see cref="IPoolable"/> on the instance, including inactive children.</summary>
        private void Awake() => _receivers = GetComponentsInChildren<IPoolable>(true);

        /// <summary>Ties the instance to the pool that created it.</summary>
        internal void Bind(IObjectPool<PooledObject> pool) => _pool = pool;

        /// <summary>Marks the instance as out of its pool and notifies its receivers.</summary>
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
}
