using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Framework
{
    /// <summary>
    /// Global object pool service. It pools prefab instances and delegates prefab loading/lifetime to ResourceManager.
    /// </summary>
    public sealed class PoolManager : SubSystemBase
    {
        public override SubSystemPriority Priority => SubSystemPriority.PoolManager;

        private readonly Dictionary<string, Queue<GameObject>> _monoPools = new();
        private readonly Dictionary<GameObject, string> _objectKeyMap = new();
        private readonly Dictionary<Type, object> _purePools = new();

        private ResourceManager _resourceManager;
        private Transform _poolRoot;

        public override void Init()
        {
            _resourceManager = Global.Get<ResourceManager>();

            GameObject root = new("[PoolManager]");
            Object.DontDestroyOnLoad(root);
            _poolRoot = root.transform;
        }

        #region Mono object pool

        public GameObject Get(string key, Vector3 position = default, Quaternion rotation = default, Transform parent = null)
        {
            if (string.IsNullOrEmpty(key))
            {
                Debug.LogError("[PoolManager] Get failed: resource key is null or empty.");
                return null;
            }

            if (!_monoPools.TryGetValue(key, out Queue<GameObject> pool))
            {
                pool = new Queue<GameObject>();
                _monoPools[key] = pool;
            }

            GameObject obj = null;
            while (pool.Count > 0 && !obj)
            {
                obj = pool.Dequeue();
            }

            if (!obj)
            {
                obj = _resourceManager.Instantiate(key, position, rotation, parent);
                if (!obj) return null;
                _objectKeyMap[obj] = key;
                return obj;
            }

            obj.transform.SetParent(!parent ? _poolRoot : parent);
            obj.transform.SetPositionAndRotation(position, rotation);
            obj.SetActive(true);
            return obj;
        }

        public T Get<T>(string key, Vector3 position = default, Quaternion rotation = default, Transform parent = null) where T : Component
        {
            GameObject obj = Get(key, position, rotation, parent);
            return obj ? obj.GetComponent<T>() : null;
        }

        public void Prewarm(string key, int count, Transform parent = null)
        {
            if (string.IsNullOrEmpty(key) || count <= 0) return;

            List<GameObject> instances = new(count);
            for (int i = 0; i < count; i++)
            {
                GameObject obj = Get(key, parent: parent);
                if (!obj) continue;
                instances.Add(obj);
            }

            foreach (GameObject obj in instances)
            {
                Release(obj);
            }
        }

        public void Release(GameObject obj)
        {
            if (!obj) return;

            if (!_objectKeyMap.TryGetValue(obj, out string key))
            {
                Debug.LogError("[PoolManager] Object was not created by this pool, destroying it directly.");
                Object.Destroy(obj);
                return;
            }

            if (!_monoPools.TryGetValue(key, out Queue<GameObject> pool))
            {
                pool = new Queue<GameObject>();
                _monoPools[key] = pool;
            }

            obj.SetActive(false);
            obj.transform.SetParent(_poolRoot);
            pool.Enqueue(obj);
        }

        public void ClearPool(string key)
        {
            ClearPoolInternal(key, true);
        }

        private void ClearPoolInternal(string key, bool clearUnusedAssets)
        {
            if (string.IsNullOrEmpty(key)) return;

            if (_monoPools.TryGetValue(key, out Queue<GameObject> pool))
            {
                while (pool.Count > 0)
                {
                    GameObject obj = pool.Dequeue();
                    if (!obj) continue;
                    _objectKeyMap.Remove(obj);
                    Object.Destroy(obj);
                }

                _monoPools.Remove(key);
            }

            _resourceManager?.ReleaseManagedCache(key);
            if (clearUnusedAssets)
            {
                _resourceManager?.ClearUnused();
            }
        }

        public void ClearPoolsByPrefix(string keyPrefix)
        {
            if (string.IsNullOrEmpty(keyPrefix)) return;

            List<string> keys = _monoPools.Keys
                .Where(key => key.StartsWith(keyPrefix, StringComparison.Ordinal))
                .ToList();

            foreach (string key in keys)
            {
                ClearPool(key);
            }
        }

        public void ClearAllMonoPools()
        {
            foreach (string key in _monoPools.Keys.ToList())
            {
                ClearPoolInternal(key, false);
            }

            _objectKeyMap.Clear();
            _resourceManager?.ClearUnused();
        }

        #endregion

        #region Pure C# object pool

        public void RegisterPool<T>(Func<T> createFunc, Action<T> onGet = null, Action<T> onRelease = null,
            Action<T> onDestroy = null, int defaultCapacity = 0, int maxCount = 32) where T : class
        {
            Type type = typeof(T);

            if (_purePools.ContainsKey(type))
            {
                Debug.LogWarning($"[PoolManager] Pool<{type.Name}> already exists.");
                return;
            }

            ObjectPool<T> pool = new(createFunc, onGet, onRelease, onDestroy, defaultCapacity, maxCount);
            _purePools[type] = pool;
        }

        public T Get<T>() where T : class
        {
            Type type = typeof(T);

            if (!_purePools.TryGetValue(type, out object poolObj))
            {
                Debug.LogError($"[PoolManager] Pool<{type.Name}> not registered.");
                return null;
            }

            return ((ObjectPool<T>)poolObj).Get();
        }

        public void Release<T>(T obj) where T : class
        {
            if (obj == null) return;

            Type type = typeof(T);

            if (!_purePools.TryGetValue(type, out object poolObj))
            {
                Debug.LogError($"[PoolManager] Pool<{type.Name}> not registered.");
                return;
            }

            ((ObjectPool<T>)poolObj).Release(obj);
        }

        public void ClearPool<T>() where T : class
        {
            Type type = typeof(T);

            if (!_purePools.TryGetValue(type, out object poolObj))
            {
                return;
            }

            ((ObjectPool<T>)poolObj).Clear();
            _purePools.Remove(type);
        }

        public void ClearAllPools()
        {
            foreach (object poolObj in _purePools.Values)
            {
                if (poolObj is IObjectPool pool)
                {
                    pool.Clear();
                }
            }

            _purePools.Clear();
        }

        #endregion

        public void ClearAll()
        {
            ClearAllMonoPools();
            ClearAllPools();
        }

        public override void Destroy()
        {
            ClearAll();

            if (_poolRoot)
            {
                Object.Destroy(_poolRoot.gameObject);
                _poolRoot = null;
            }
            
            _resourceManager = null;
        }
    }
}
