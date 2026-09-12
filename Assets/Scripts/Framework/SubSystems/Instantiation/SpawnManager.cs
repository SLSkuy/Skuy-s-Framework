using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Core;
using UnityEngine;
using YooAsset;
using Object = UnityEngine.Object;

namespace Framework
{
    /// <summary>
    /// 按资源位置生成与回收 GameObject
    /// </summary>
    public sealed class SpawnManager : SubSystemBase
    {
        #region 内部类型

        /// <summary>
        /// 一个资源位置对应的实例化池
        /// </summary>
        private sealed class SpawnEntry
        {
            /// <summary>
            /// 该资源位置对应的预制体句柄
            /// </summary>
            public AssetHandle Handle;

            /// <summary>
            /// 当前空闲实例
            /// </summary>
            public readonly Queue<GameObject> Idle = new();

            /// <summary>
            /// 当前借出的实例数量
            /// </summary>
            public int LentCount;

            /// <summary>
            /// 当前等待资源加载的请求数量
            /// </summary>
            public int PendingCount;

            /// <summary>
            /// 资源完全空闲后的累计时间
            /// </summary>
            public float IdleElapsed;
        }

        /// <summary>
        /// 等待完成的异步实例化请求
        /// </summary>
        private sealed class PendingInstantiate
        {
            public readonly string Location;
            public readonly InstantiateOptions Options;
            public readonly TaskCompletionSource<GameObject> Completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
            
            public PendingInstantiate(string location, InstantiateOptions options)
            {
                Location = location;
                Options = options;
            }
        }

        #endregion

        #region 字段

        private ResourceManager _resourceManager;
        private Transform _poolRoot;

        /// <summary>
        /// 按资源位置管理资源句柄、对象池以及相关状态
        /// </summary>
        private readonly Dictionary<string, SpawnEntry> _entries = new();

        /// <summary>
        /// 记录当前借出的实例及其所属资源池
        /// </summary>
        private readonly Dictionary<GameObject, SpawnEntry> _lent = new();

        /// <summary>
        /// 不进池的实例：Release 时销毁
        /// </summary>
        private readonly HashSet<GameObject> _unpooled = new();

        /// <summary>
        /// 等待资源加载完成的异步实例化请求
        /// </summary>
        private readonly List<PendingInstantiate> _pending = new();

        /// <summary>
        /// 空闲资源卸载检查的临时列表
        /// </summary>
        private readonly List<string> _unloadScratch = new();

        private float _idleUnloadSeconds;

        #endregion

        #region 属性

        public override int Priority => (int)SubSystemPriority.InstantiationManager;

        #endregion

        #region 公共接口

        /// <summary>
        /// 按资源位置同步生成实例。
        /// </summary>
        public GameObject Instantiate(string location, InstantiateOptions options = default)
        {
            return InstantiateFromLocation(location, options, instance => instance.transform.SetParent(null));
        }

        /// <summary>
        /// 按资源位置异步生成实例。
        /// </summary>
        public Task<GameObject> InstantiateAsync(string location, InstantiateOptions options = default)
        {
            return InstantiateAsyncFromLocation(location, options);
        }

        /// <summary>
        /// 按资源位置同步生成不进池的实例。
        /// Release 时销毁；切场景 Clear 不会回收。
        /// </summary>
        public GameObject InstantiateUnpooled(string location, InstantiateOptions options = default)
        {
            if (!ValidateLocation(location)) return null;

            AssetHandle handle = ResolveHandleForUnpooled(location, out bool disposeHandle);
            if (handle == null) return null;

            GameObject instance = handle.InstantiateSync(options);
            if (disposeHandle) handle.Dispose();

            if (!instance)
            {
                LogInstantiateFailed(location);
                return null;
            }

            _unpooled.Add(instance);
            return instance;
        }

        /// <summary>
        /// 回收由本管理器生成的实例。
        /// 池化实例还池；不进池实例销毁。外源、空引用或重复 Release 会记录错误并忽略。
        /// </summary>
        public void Release(GameObject instance)
        {
            if (!instance)
            {
                Debug.LogError($"[{nameof(SpawnManager)}] Release ignored: instance is null.");
                return;
            }

            if (_unpooled.Remove(instance))
            {
                DestroyInstance(instance);
                return;
            }

            if (!_lent.Remove(instance, out SpawnEntry entry))
            {
                Debug.LogError($"[{nameof(SpawnManager)}] Release ignored: instance was not created by the instance facade.");
                return;
            }

            instance.SetActive(false);
            instance.transform.SetParent(_poolRoot);
            entry.Idle.Enqueue(instance);
            entry.LentCount--;

            if (entry.LentCount == 0 && entry.PendingCount == 0) entry.IdleElapsed = 0f;
        }

        /// <summary>
        /// 销毁全部借出与空闲实例，并释放所有资源句柄
        /// </summary>
        public void Clear()
        {
            ClearPending();
            ClearLentInstances();
            ClearIdleInstances();
            ClearEntries();
            
            Debug.Log($"[{nameof(SpawnManager)}] 已清空所有池化对象");
        }

        #endregion

        #region 同步实例化

        /// <summary>
        /// 同步执行预制体实例化。
        /// </summary>
        private GameObject InstantiateFromLocation(string location, InstantiateOptions options, Action<GameObject> activate)
        {
            if (!ValidateLocation(location)) return null;

            GameObject pooled = RentFromPool(location, activate);
            if (pooled) return pooled;

            SpawnEntry entry = GetOrCreateEntry(location);
            AssetHandle handle = GetOrLoadHandle(location, entry, false);

            if (handle == null) return null;

            return InstantiateFromHandle(entry, handle, options);
        }

        #endregion

        #region 异步实例化

        /// <summary>
        /// 异步执行预制体实例化。
        /// </summary>
        private Task<GameObject> InstantiateAsyncFromLocation(string location, InstantiateOptions options)
        {
            if (!ValidateLocation(location)) return Task.FromResult<GameObject>(null);

            GameObject pooled = RentFromPool(location, instance => instance.transform.SetParent(null));
            if (pooled) return Task.FromResult(pooled);

            SpawnEntry entry = GetOrCreateEntry(location);
            PendingInstantiate pending = new(location, options);

            entry.PendingCount++;

            AssetHandle handle = GetOrLoadHandle(location, entry, true);
            if (handle == null)
            {
                entry.PendingCount--;
                pending.Completion.TrySetResult(null);
                RemoveEntryIfUnused(location, entry);
                return pending.Completion.Task;
            }

            _pending.Add(pending);
            return pending.Completion.Task;
        }

        /// <summary>
        /// 尝试完成一个异步实例化请求。
        /// </summary>
        private bool TryCompletePending(PendingInstantiate pending)
        {
            if (!_entries.TryGetValue(pending.Location, out SpawnEntry entry))
            {
                pending.Completion.TrySetResult(null);
                return true;
            }

            AssetHandle handle = entry.Handle;
            if (handle == null)
            {
                entry.PendingCount--;
                LogInstantiateFailed(pending.Location);
                pending.Completion.TrySetResult(null);
                RemoveEntryIfUnused(pending.Location, entry);
                return true;
            }

            if (!handle.IsDone)
                return false;

            if (handle.AssetObject is not GameObject)
            {
                entry.PendingCount--;
                LogInstantiateFailed(pending.Location);
                handle.Dispose();
                entry.Handle = null;
                pending.Completion.TrySetResult(null);
                RemoveEntryIfUnused(pending.Location, entry);
                return true;
            }

            GameObject instance = InstantiateFromHandle(entry, handle, pending.Options);
            entry.PendingCount--;

            if (!instance)
            {
                LogInstantiateFailed(pending.Location);
                pending.Completion.TrySetResult(null);
                RemoveEntryIfUnused(pending.Location, entry);
                return true;
            }

            pending.Completion.TrySetResult(instance);
            return true;
        }

        #endregion

        #region 对象池

        /// <summary>
        /// 尝试从对象池借出一个实例。
        /// </summary>
        private GameObject RentFromPool(string location, Action<GameObject> activate)
        {
            if (!_entries.TryGetValue(location, out SpawnEntry entry)) return null;

            while (entry.Idle.Count > 0)
            {
                GameObject instance = entry.Idle.Dequeue();

                if (!instance)
                    continue;

                activate(instance);
                instance.SetActive(true);

                entry.LentCount++;
                _lent[instance] = entry;
                entry.IdleElapsed = 0f;

                return instance;
            }

            return null;
        }

        /// <summary>
        /// 从资源句柄实例化 GameObject，并登记为借出实例。
        /// </summary>
        private GameObject InstantiateFromHandle(SpawnEntry entry, AssetHandle handle, InstantiateOptions options)
        {
            GameObject instance = handle.InstantiateSync(options);
            if (!instance) return null;

            entry.LentCount++;
            _lent[instance] = entry;

            return instance;
        }

        #endregion

        #region 资源管理

        /// <summary>
        /// 获取或加载指定资源位置的预制体句柄。
        /// </summary>
        private AssetHandle GetOrLoadHandle(string location, SpawnEntry entry, bool async)
        {
            if (entry.Handle != null) return entry.Handle;
            AssetHandle handle = async ? _resourceManager.LoadAsync<GameObject>(location) : _resourceManager.Load<GameObject>(location);

            if (handle == null || !handle.IsValid)
            {
                if (!async) LogInstantiateFailed(location);
                return null;
            }
            
            entry.Handle = handle;
            if (!handle.IsDone) return handle;
            if (handle.AssetObject is not GameObject)
            {
                if (!async)
                    LogInstantiateFailed(location);

                handle.Dispose();
                entry.Handle = null;
                return null;
            }

            return handle;
        }

        /// <summary>
        /// 不进池实例化使用已缓存的预制体句柄，否则加载一次并在实例化后释放。
        /// </summary>
        private AssetHandle ResolveHandleForUnpooled(string location, out bool disposeHandle)
        {
            if (_entries.TryGetValue(location, out SpawnEntry entry) && entry.Handle != null && entry.Handle.IsValid
                && entry.Handle.IsDone && entry.Handle.AssetObject is GameObject)
            {
                disposeHandle = false;
                return entry.Handle;
            }

            AssetHandle handle = _resourceManager.Load<GameObject>(location);
            if (handle == null || !handle.IsValid || handle.AssetObject is not GameObject)
            {
                LogInstantiateFailed(location);
                handle?.Dispose();
                disposeHandle = false;
                return null;
            }

            disposeHandle = true;
            return handle;
        }

        /// <summary>
        /// 卸载一个已经完全空闲的资源池。
        /// </summary>
        private void UnloadEntry(string location, SpawnEntry entry)
        {
            while (entry.Idle.Count > 0) DestroyInstance(entry.Idle.Dequeue());
            entry.Handle?.Dispose();
            entry.Handle = null;

            _entries.Remove(location);
        }

        /// <summary>
        /// 当资源池已经没有任何使用者时移除资源池。
        /// </summary>
        private void RemoveEntryIfUnused(string location, SpawnEntry entry)
        {
            if (entry.LentCount > 0 || entry.PendingCount > 0 || entry.Idle.Count > 0)
                return;

            if (_entries.TryGetValue(location, out SpawnEntry current) && ReferenceEquals(current, entry))
            {
                entry.Handle?.Dispose();
                entry.Handle = null;
                _entries.Remove(location);
            }
        }

        #endregion

        #region Pending

        /// <summary>
        /// 清理所有等待中的异步实例化请求。
        /// </summary>
        private void ClearPending()
        {
            for (int i = 0; i < _pending.Count; i++)
            {
                PendingInstantiate pending = _pending[i];

                if (pending.Completion.Task.IsCompleted)
                    continue;

                pending.Completion.TrySetResult(null);
            }

            _pending.Clear();
        }

        #endregion

        #region 空闲卸载

        private void TickIdleUnload(float deltaTime)
        {
            _unloadScratch.Clear();

            foreach (KeyValuePair<string, SpawnEntry> pair in _entries)
            {
                SpawnEntry entry = pair.Value;

                if (entry.LentCount > 0 || entry.PendingCount > 0)
                {
                    entry.IdleElapsed = 0f;
                    continue;
                }

                if (entry.Idle.Count == 0)
                {
                    entry.IdleElapsed = 0f;
                    continue;
                }

                entry.IdleElapsed += deltaTime;

                if (entry.IdleElapsed >= _idleUnloadSeconds) _unloadScratch.Add(pair.Key);
            }

            foreach (var location in _unloadScratch)
            {
                if (!_entries.TryGetValue(location, out SpawnEntry entry)) continue;
                if (entry.LentCount > 0 || entry.PendingCount > 0) continue;
                UnloadEntry(location, entry);
            }

            _unloadScratch.Clear();
        }

        #endregion

        #region 查询

        private SpawnEntry GetOrCreateEntry(string location)
        {
            if (_entries.TryGetValue(location, out SpawnEntry entry)) return entry;
            entry = new SpawnEntry();
            _entries.Add(location, entry);

            return entry;
        }

        private static bool ValidateLocation(string location)
        {
            if (!string.IsNullOrEmpty(location)) return true;
            
            LogInstantiateFailed(location);
            return false;
        }

        private static void LogInstantiateFailed(string location)
        {
            if (string.IsNullOrEmpty(location))
                Debug.LogError("[SpawnManager] Instantiate failed: asset location is empty.");
            else
                Debug.LogError($"[SpawnManager] Instantiate failed: '{location}'.");
        }

        #endregion

        #region 清理

        private void ClearLentInstances()
        {
            foreach (GameObject instance in _lent.Keys) DestroyInstance(instance);
            
            _lent.Clear();
        }

        private void ClearIdleInstances()
        {
            foreach (SpawnEntry entry in _entries.Values)
            {
                while (entry.Idle.Count > 0) DestroyInstance(entry.Idle.Dequeue());
            }
        }

        private void ClearEntries()
        {
            foreach (SpawnEntry entry in _entries.Values)
                entry.Handle?.Dispose();

            _entries.Clear();
        }

        private void ClearUnpooledInstances()
        {
            foreach (GameObject instance in _unpooled)
                DestroyInstance(instance);

            _unpooled.Clear();
        }

        private static void DestroyInstance(GameObject instance)
        {
            if (!instance) return;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Object.DestroyImmediate(instance);
                return;
            }
#endif
            
            Object.Destroy(instance);
        }

        #endregion

        #region 子系统生命周期

        public override void Init()
        {
            _resourceManager = Global.Get<ResourceManager>();

            _poolRoot = new GameObject($"[{nameof(SpawnManager)}]").transform;
            _poolRoot.SetParent(GameObject.Find("[GameRoot]").transform);

            _idleUnloadSeconds = GameCoreConfig.Instance.clearUnusedIdleTime;
        }

        public override void Update(float deltaTime)
        {
            int pendingCount = _pending.Count;

            for (int i = 0; i < pendingCount;)
            {
                PendingInstantiate pending = _pending[i];

                if (!TryCompletePending(pending))
                {
                    i++;
                    continue;
                }

                _pending.RemoveAt(i);
                pendingCount--;
            }

            TickIdleUnload(deltaTime);
        }

        public override void Destroy()
        {
            Clear();
            ClearUnpooledInstances();

            if (_poolRoot)
            {
                DestroyInstance(_poolRoot.gameObject);
                _poolRoot = null;
            }

            _resourceManager = null;
        }

        #endregion
    }
}