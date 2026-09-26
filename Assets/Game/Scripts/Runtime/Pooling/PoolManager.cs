using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;

namespace BattleArena.Pooling
{
    /// <summary>
    /// Prefab-keyed object pools built on <see cref="ObjectPool{T}"/>. Pools are scene-scoped:
    /// they are dropped when a scene unloads because their instances are destroyed with it.
    /// </summary>
    public static class PoolManager
    {
        private const int DefaultCapacity = 16;
        private const int MaxPoolSize = 512;

        private static readonly Dictionary<GameObject, ObjectPool<PooledObject>> Pools = new();
        private static Transform _root;

        // New instances are created at the requested pose so Awake/OnEnable (e.g. NavMeshAgent) see the right position.
        private static Vector3 _pendingPosition;
        private static Quaternion _pendingRotation;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Initialize()
        {
            Pools.Clear();
            _root = null;
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
        }

        private static void OnSceneUnloaded(Scene _)
        {
            Pools.Clear();
            _root = null;
        }

        public static T Spawn<T>(T prefab, Vector3 position, Quaternion rotation) where T : Component
        {
            _pendingPosition = position;
            _pendingRotation = rotation;

            var instance = GetPool(prefab.gameObject).Get();
            if (!instance.gameObject.activeSelf)
            {
                instance.transform.SetPositionAndRotation(position, rotation);
                instance.gameObject.SetActive(true);
            }

            instance.NotifySpawned();
            return instance.GetComponent<T>();
        }

        /// <summary>Instantiates <paramref name="count"/> inactive instances up front to avoid hitches mid-game.</summary>
        public static void Prewarm(Component prefab, int count, Vector3 position)
        {
            var pool = GetPool(prefab.gameObject);
            if (pool.CountInactive >= count)
                return;

            _pendingPosition = position;
            _pendingRotation = Quaternion.identity;

            // Get() drains the inactive instances first, then creates the rest.
            var buffer = ListPool<PooledObject>.Get();
            for (var i = 0; i < count; i++)
                buffer.Add(pool.Get());
            foreach (var instance in buffer)
                pool.Release(instance);
            ListPool<PooledObject>.Release(buffer);
        }

        private static ObjectPool<PooledObject> GetPool(GameObject prefab)
        {
            if (Pools.TryGetValue(prefab, out var pool))
                return pool;

            ObjectPool<PooledObject> created = null;
            created = new ObjectPool<PooledObject>(
                createFunc: () => CreateInstance(prefab, created),
                actionOnRelease: instance => instance.gameObject.SetActive(false),
                actionOnDestroy: instance =>
                {
                    if (instance != null)
                        Object.Destroy(instance.gameObject);
                },
                collectionCheck: false,
                defaultCapacity: DefaultCapacity,
                maxSize: MaxPoolSize);

            Pools.Add(prefab, created);
            return created;
        }

        private static PooledObject CreateInstance(GameObject prefab, IObjectPool<PooledObject> pool)
        {
            var instance = Object.Instantiate(prefab, _pendingPosition, _pendingRotation, GetRoot());
            if (!instance.TryGetComponent(out PooledObject pooled))
                pooled = instance.AddComponent<PooledObject>();
            pooled.Bind(pool);
            return pooled;
        }

        private static Transform GetRoot()
        {
            if (_root == null)
                _root = new GameObject("[Pools]").transform;
            return _root;
        }
    }
}
