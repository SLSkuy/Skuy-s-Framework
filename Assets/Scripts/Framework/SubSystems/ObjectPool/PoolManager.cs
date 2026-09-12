using System;
using System.Collections.Generic;
using UnityEngine;

namespace Framework
{
    /// <summary>
    /// 进程级纯 C# 对象池。不按资源位置取还 GameObject，不向资源门面要实例。
    /// </summary>
    public sealed class PoolManager : SubSystemBase
    {
        private readonly Dictionary<Type, object> _purePools = new();

        #region 属性
        public override int Priority => (int)SubSystemPriority.PoolManager;
        #endregion

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

        #region 子系统生命周期
        public override void Destroy()
        {
            ClearAllPools();
        }
        #endregion
    }
}
