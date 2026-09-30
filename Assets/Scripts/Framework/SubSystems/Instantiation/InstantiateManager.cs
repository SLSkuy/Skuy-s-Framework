using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using YooAsset;
using Object = UnityEngine.Object;

namespace Framework
{
    /// <summary>
    /// 按资源位置生成与回收 GameObject
    /// </summary>
    public sealed class InstantiateManager : SubSystemBase
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
            public readonly Queue<InstanceEntry> Idle = new();

            /// <summary>
            /// 当前借出的实例数量
            /// </summary>
            public int LentCount;

            /// <summary>
            /// 当前等待资源加载的请求
            /// </summary>
            public readonly List<PendingInstantiate> Pending = new();

            /// <summary>
            /// 当前等待资源加载的请求数量
            /// </summary>
            public int PendingCount;

            /// <summary>
            /// 资源完全空闲后的累计时间
            /// </summary>
            public float IdleElapsed;

            /// <summary>
            /// 该资源位置第一次建池时确定的分组
            /// </summary>
            public ResGroup Group;

            /// <summary>
            /// 建池时记下的分组策略
            /// </summary>
            public ResGroupPolicy.Setting Policy;

            /// <summary>
            /// 是否已经按策略预热过
            /// </summary>
            public bool Prewarmed;

            /// <summary>
            /// 句柄完成回调。只绑定一次，句柄替换后重新绑定。
            /// </summary>
            public Action<AssetHandle> OnCompleted;

            public bool CompletionBound;
        }

        /// <summary>
        /// 一个池化实例的元数据。IPoolable 在创建时缓存，借还不再查找组件。
        /// 分组策略不要求 IPoolable 时该数组可以为空，复位交给业务自己做。
        /// </summary>
        private sealed class InstanceEntry
        {
            public GameObject Instance;
            public SpawnEntry Spawn;
            public IPoolable[] Poolables;
        }

        /// <summary>
        /// 等待完成的异步实例化请求
        /// </summary>
        private sealed class PendingInstantiate
        {
            public readonly string Location;
            public readonly ResGroup Group;
            public readonly InstantiateOptions Options;
            public readonly TaskCompletionSource<GameObject> Completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
            
            public PendingInstantiate(string location, ResGroup group, InstantiateOptions options)
            {
                Location = location;
                Group = group;
                Options = options;
            }
        }

        #endregion

        #region 字段

        private ResourceManager _resourceManager;
        private Transform _poolRoot;
        private Transform[] _groupRoots;

        /// <summary>
        /// 按资源位置管理资源句柄、对象池以及相关状态
        /// </summary>
        private readonly Dictionary<string, SpawnEntry> _entries = new();

        /// <summary>
        /// 记录当前借出的实例及其元数据
        /// </summary>
        private readonly Dictionary<GameObject, InstanceEntry> _lent = new();

        /// <summary>
        /// 不进池的实例：Release 时销毁
        /// </summary>
        private readonly HashSet<GameObject> _unpooled = new();

        /// <summary>
        /// 空闲资源卸载检查的临时列表
        /// </summary>
        private readonly List<string> _unloadScratch = new();

        /// <summary>
        /// 切场景时收集待销毁借出实例
        /// </summary>
        private readonly List<GameObject> _lentScratch = new();

        #endregion

        #region 属性

        public override int Priority => (int)SubSystemPriority.SpawnManager;

        #endregion

        #region 公共接口

        // ReSharper disable Unity.PerformanceAnalysis
        /// <summary>
        /// 按资源位置同步生成实例，分组为 <see cref="ResGroup.Temp"/>。
        /// </summary>
        public GameObject Instantiate(string location, InstantiateOptions options = default)
        {
            return Instantiate(location, ResGroup.Temp, options);
        }

        /// <summary>
        /// 按资源位置和分组同步生成实例。同一资源名第一次建池时确定分组。
        /// </summary>
        public GameObject Instantiate(string location, ResGroup group, InstantiateOptions options = default)
        {
            return InstantiateFromLocation(location, group, options);
        }

        /// <summary>
        /// 按资源位置异步生成实例，分组为 <see cref="ResGroup.Temp"/>。
        /// </summary>
        public Task<GameObject> InstantiateAsync(string location, InstantiateOptions options = default)
        {
            return InstantiateAsync(location, ResGroup.Temp, options);
        }

        /// <summary>
        /// 按资源位置和分组异步生成实例。同一资源名第一次建池时确定分组。
        /// </summary>
        public Task<GameObject> InstantiateAsync(string location, ResGroup group, InstantiateOptions options = default)
        {
            return InstantiateAsyncFromLocation(location, group, options);
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

        // ReSharper disable Unity.PerformanceAnalysis
        /// <summary>
        /// 回收由本管理器生成的实例。
        /// 池化实例还池；不进池实例销毁。外源、空引用或重复 Release 会记录错误并忽略。
        /// </summary>
        public void Release(GameObject instance)
        {
            if (!instance)
            {
                Debug.LogError($"[{nameof(InstantiateManager)}] Release ignored: instance is null.");
                return;
            }

            if (_unpooled.Remove(instance))
            {
                DestroyInstance(instance);
                return;
            }

            if (!_lent.TryGetValue(instance, out InstanceEntry tracked))
            {
                Debug.LogError($"[{nameof(InstantiateManager)}] Release ignored: instance was not created by the instance facade.");
                return;
            }

            SpawnEntry entry = tracked.Spawn;
            _lent.Remove(instance);
            entry.LentCount--;
            try
            {
                ResetPooledInstance(tracked);
            }
            finally
            {
                if (entry.Idle.Count >= entry.Policy.maxIdleCount)
                    DestroyInstance(instance);
                else
                {
                    instance.SetActive(false);
                    instance.transform.SetParent(_groupRoots[(int)entry.Group]);
                    entry.Idle.Enqueue(tracked);
                }

                if (entry.LentCount == 0 && entry.PendingCount == 0)
                    entry.IdleElapsed = 0f;
            }
        }

        // ReSharper disable Unity.PerformanceAnalysis
        /// <summary>
        /// 切场景时销毁策略里 sceneClear 的分组。卸载路径与空闲超时相同。
        /// </summary>
        public void ClearSceneGroups()
        {
            CancelScenePending();
            DestroySceneLent();

            _unloadScratch.Clear();
            foreach (KeyValuePair<string, SpawnEntry> pair in _entries)
            {
                if (pair.Value.Policy.sceneClear)
                    _unloadScratch.Add(pair.Key);
            }

            for (int i = 0; i < _unloadScratch.Count; i++)
            {
                string location = _unloadScratch[i];
                if (!_entries.TryGetValue(location, out SpawnEntry entry))
                    continue;

                TryUnloadEntry(location, entry);
            }

            _unloadScratch.Clear();
        }

        /// <summary>
        /// 销毁全部借出与空闲实例，并释放所有资源句柄
        /// </summary>
        public void ClearAll()
        {
            ClearPending();
            ClearLentInstances();
            ClearIdleInstances();
            ClearEntries();
            
            Debug.Log($"[{nameof(InstantiateManager)}] 已清空所有池化对象");
        }

        #endregion

        #region 同步实例化

        // ReSharper disable Unity.PerformanceAnalysis
        /// <summary>
        /// 同步执行预制体实例化。
        /// </summary>
        private GameObject InstantiateFromLocation(string location, ResGroup group, InstantiateOptions options)
        {
            if (!ValidateLocation(location)) return null;
            if (!TryGetOrCreateEntry(location, group, out SpawnEntry entry)) return null;

            GameObject pooled = RentFromPool(entry, options);
            if (pooled) return pooled;

            AssetHandle handle = GetOrLoadHandle(location, entry, false);
            if (handle == null)
            {
                RemoveUnusedEntry(location, entry);
                return null;
            }

            Prewarm(entry, handle);
            pooled = RentFromPool(entry, options);
            if (pooled) return pooled;

            return InstantiateFromHandle(entry, handle, options);
        }

        #endregion

        #region 异步实例化

        /// <summary>
        /// 异步执行预制体实例化。
        /// </summary>
        private Task<GameObject> InstantiateAsyncFromLocation(string location, ResGroup group, InstantiateOptions options)
        {
            if (!ValidateLocation(location)) return Task.FromResult<GameObject>(null);
            if (!TryGetOrCreateEntry(location, group, out SpawnEntry entry))
                return Task.FromResult<GameObject>(null);

            GameObject pooled = RentFromPool(entry, options);
            if (pooled) return Task.FromResult(pooled);

            PendingInstantiate pending = new(location, group, options);
            entry.Pending.Add(pending);
            entry.PendingCount++;

            AssetHandle handle = GetOrLoadHandle(location, entry, true);
            if (handle == null)
            {
                entry.Pending.Remove(pending);
                entry.PendingCount--;
                pending.Completion.TrySetResult(null);
                RemoveUnusedEntry(location, entry);
                return pending.Completion.Task;
            }

            if (handle.IsDone)
                CompletePending(entry);
            else
                EnsureCompletion(entry);

            return pending.Completion.Task;
        }

        /// <summary>
        /// 句柄完成时，结束该资源池上的全部等待请求。
        /// </summary>
        private void EnsureCompletion(SpawnEntry entry)
        {
            if (entry.Handle == null || entry.CompletionBound)
                return;

            entry.OnCompleted ??= _ => CompletePending(entry);
            entry.CompletionBound = true;
            entry.Handle.Completed += entry.OnCompleted;
        }

        private void CompletePending(SpawnEntry entry)
        {
            if (entry.Pending.Count == 0)
                return;

            string location = entry.Pending[0].Location;
            AssetHandle handle = entry.Handle;
            if (handle == null || !handle.IsValid || handle.AssetObject is not GameObject)
            {
                LogInstantiateFailed(location);
                FailPending(entry);
                DisposeHandle(entry);
                RemoveUnusedEntry(location, entry);
                return;
            }

            Prewarm(entry, handle);

            while (entry.Pending.Count > 0)
            {
                PendingInstantiate pending = entry.Pending[0];
                entry.Pending.RemoveAt(0);
                entry.PendingCount--;

                if (pending.Group != entry.Group)
                {
                    Debug.LogError($"[{nameof(InstantiateManager)}] '{pending.Location}' is already in {entry.Group}, refused {pending.Group}.");
                    pending.Completion.TrySetResult(null);
                    continue;
                }

                GameObject instance = RentFromPool(entry, pending.Options);
                if (!instance)
                    instance = InstantiateFromHandle(entry, handle, pending.Options);
                if (!instance)
                {
                    LogInstantiateFailed(pending.Location);
                    pending.Completion.TrySetResult(null);
                    continue;
                }

                pending.Completion.TrySetResult(instance);
            }

            RemoveUnusedEntry(location, entry);
        }

        private void FailPending(SpawnEntry entry)
        {
            for (int i = 0; i < entry.Pending.Count; i++)
            {
                PendingInstantiate pending = entry.Pending[i];
                if (!pending.Completion.Task.IsCompleted)
                    pending.Completion.TrySetResult(null);
            }

            entry.PendingCount = 0;
            entry.Pending.Clear();
        }

        #endregion

        #region 对象池

        /// <summary>
        /// 尝试从对象池借出一个实例。
        /// </summary>
        private GameObject RentFromPool(SpawnEntry entry, InstantiateOptions options)
        {
            while (entry.Idle.Count > 0)
            {
                InstanceEntry tracked = entry.Idle.Dequeue();
                GameObject instance = tracked.Instance;

                if (!instance)
                    continue;

                ResetPooledInstance(tracked);
                ApplyOptions(instance, options);

                entry.LentCount++;
                _lent[instance] = tracked;
                entry.IdleElapsed = 0f;

                return instance;
            }

            return null;
        }

        /// <summary>
        /// 借出时套用与新建实例相同的父节点、位姿和激活状态。无父节点时放入当前激活场景。
        /// </summary>
        private static void ApplyOptions(GameObject instance, InstantiateOptions options)
        {
            if (options.Parent)
                instance.transform.SetParent(options.Parent, options.InWorldSpace);
            else
            {
                instance.transform.SetParent(null);
                SceneManager.MoveGameObjectToScene(instance, SceneManager.GetActiveScene());
            }

            instance.transform.SetPositionAndRotation(options.Position, options.Rotation);
            instance.SetActive(options.IsActive);
        }

        /// <summary>
        /// 还池或再次借出前重置。组件列表在创建时缓存。
        /// </summary>
        private static void ResetPooledInstance(InstanceEntry tracked)
        {
            IPoolable[] poolables = tracked.Poolables;
            for (int i = 0; i < poolables.Length; i++)
                poolables[i].Reset();
        }

        /// <summary>
        /// 从资源句柄实例化 GameObject，并登记为借出实例。
        /// </summary>
        private GameObject InstantiateFromHandle(SpawnEntry entry, AssetHandle handle, InstantiateOptions options)
        {
            InstanceEntry tracked = CreateTrackedInstance(entry, handle, options);
            if (tracked == null) return null;

            entry.LentCount++;
            _lent[tracked.Instance] = tracked;

            return tracked.Instance;
        }

        /// <summary>
        /// 按策略把空闲实例预热进池。只执行一次，数量不超过空闲上限。
        /// </summary>
        private void Prewarm(SpawnEntry entry, AssetHandle handle)
        {
            if (entry.Prewarmed)
                return;

            entry.Prewarmed = true;
            int count = entry.Policy.prewarmCount;
            if (count > entry.Policy.maxIdleCount)
                count = entry.Policy.maxIdleCount;

            InstantiateOptions options = new(false, _groupRoots[(int)entry.Group], false);
            for (int i = 0; i < count; i++)
            {
                InstanceEntry tracked = CreateTrackedInstance(entry, handle, options);
                if (tracked == null)
                    break;

                entry.Idle.Enqueue(tracked);
            }
        }

        private InstanceEntry CreateTrackedInstance(SpawnEntry entry, AssetHandle handle, InstantiateOptions options)
        {
            GameObject instance = handle.InstantiateSync(options);
            if (!instance) return null;

            IPoolable[] poolables = instance.GetComponentsInChildren<IPoolable>(true);
            if (poolables.Length == 0 && entry.Policy.requirePoolable)
            {
                DestroyInstance(instance);
                throw new InvalidOperationException($"实例 {instance.name} 未实现 {nameof(IPoolable)}，不能进入 {entry.Group} 池。");
            }

            return new InstanceEntry
            {
                Instance = instance,
                Spawn = entry,
                Poolables = poolables
            };
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
                if (!async) LogInstantiateFailed(location);

                DisposeHandle(entry);
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

        // ReSharper disable Unity.PerformanceAnalysis
        /// <summary>
        /// 卸载已经没有借出和等待加载的资源池。仍在使用时不释放句柄。
        /// </summary>
        private bool TryUnloadEntry(string location, SpawnEntry entry)
        {
            if (entry.LentCount > 0 || entry.PendingCount > 0)
                return false;

            while (entry.Idle.Count > 0)
                DestroyInstance(entry.Idle.Dequeue().Instance);

            DisposeHandle(entry);
            _entries.Remove(location);
            return true;
        }

        /// <summary>
        /// 当资源池已经没有任何使用者时移除资源池。
        /// </summary>
        private void RemoveUnusedEntry(string location, SpawnEntry entry)
        {
            if (entry.LentCount > 0 || entry.PendingCount > 0 || entry.Idle.Count > 0)
                return;

            if (!_entries.TryGetValue(location, out SpawnEntry current) || !ReferenceEquals(current, entry))
                return;

            DisposeHandle(entry);
            _entries.Remove(location);
        }

        private void DisposeHandle(SpawnEntry entry)
        {
            AssetHandle handle = entry.Handle;
            if (handle == null)
                return;

            if (entry.CompletionBound && entry.OnCompleted != null)
            {
                handle.Completed -= entry.OnCompleted;
                entry.CompletionBound = false;
            }

            entry.Handle = null;
            handle.Dispose();
        }

        #endregion

        #region Pending

        private void CancelScenePending()
        {
            foreach (SpawnEntry entry in _entries.Values)
            {
                if (!entry.Policy.sceneClear)
                    continue;

                for (int i = entry.Pending.Count - 1; i >= 0; i--)
                {
                    PendingInstantiate pending = entry.Pending[i];
                    if (!pending.Completion.Task.IsCompleted)
                        pending.Completion.TrySetResult(null);

                    entry.Pending.RemoveAt(i);
                }

                entry.PendingCount = 0;
            }
        }

        /// <summary>
        /// 销毁切场景分组中仍借出的实例。场景卸载后已销毁的实例只从登记中移除。
        /// </summary>
        private void DestroySceneLent()
        {
            _lentScratch.Clear();
            foreach (KeyValuePair<GameObject, InstanceEntry> pair in _lent)
            {
                if (pair.Value.Spawn.Policy.sceneClear)
                    _lentScratch.Add(pair.Key);
            }

            for (int i = 0; i < _lentScratch.Count; i++)
            {
                GameObject instance = _lentScratch[i];
                if (!_lent.TryGetValue(instance, out InstanceEntry tracked))
                    continue;

                _lent.Remove(instance);
                tracked.Spawn.LentCount--;
                DestroyInstance(instance);
            }

            _lentScratch.Clear();
        }

        /// <summary>
        /// 清理所有等待中的异步实例化请求。
        /// </summary>
        private void ClearPending()
        {
            foreach (SpawnEntry entry in _entries.Values)
            {
                for (int i = 0; i < entry.Pending.Count; i++)
                {
                    PendingInstantiate pending = entry.Pending[i];
                    if (!pending.Completion.Task.IsCompleted)
                        pending.Completion.TrySetResult(null);
                }

                entry.Pending.Clear();
                entry.PendingCount = 0;
            }
        }

        #endregion

        #region 空闲卸载

        private void TickIdleUnload(float deltaTime)
        {
            _unloadScratch.Clear();

            foreach (KeyValuePair<string, SpawnEntry> pair in _entries)
            {
                SpawnEntry entry = pair.Value;

                if (!entry.Policy.idleUnload || entry.LentCount > 0 || entry.PendingCount > 0)
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

                if (entry.IdleElapsed >= entry.Policy.idleUnloadSeconds) _unloadScratch.Add(pair.Key);
            }

            foreach (var location in _unloadScratch)
            {
                if (!_entries.TryGetValue(location, out SpawnEntry entry)) continue;
                TryUnloadEntry(location, entry);
            }

            _unloadScratch.Clear();
        }

        #endregion

        #region 查询

        // ReSharper disable Unity.PerformanceAnalysis
        /// <summary>
        /// 取得资源池。不存在则按本次分组创建。已存在且分组不同则拒绝。
        /// </summary>
        private bool TryGetOrCreateEntry(string location, ResGroup group, out SpawnEntry entry)
        {
            if (_entries.TryGetValue(location, out entry))
            {
                if (entry.Group == group)
                    return true;

                Debug.LogError($"[{nameof(InstantiateManager)}] '{location}' is already in {entry.Group}, refused {group}.");
                entry = null;
                return false;
            }

            entry = new SpawnEntry
            {
                Group = group,
                Policy = ResGroupPolicy.Get(group)
            };
            _entries.Add(location, entry);
            return true;
        }

        private static bool ValidateLocation(string location)
        {
            if (!string.IsNullOrEmpty(location)) return true;
            
            LogInstantiateFailed(location);
            return false;
        }

        // ReSharper disable Unity.PerformanceAnalysis
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
                while (entry.Idle.Count > 0) DestroyInstance(entry.Idle.Dequeue().Instance);
            }
        }

        private void ClearEntries()
        {
            foreach (SpawnEntry entry in _entries.Values)
                DisposeHandle(entry);

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

        private void CreateGroupRoots()
        {
            int count = (int)ResGroup.Temp + 1;
            _groupRoots = new Transform[count];
            for (int i = 0; i < count; i++)
            {
                ResGroup group = (ResGroup)i;
                Transform node = new GameObject(group.ToString()).transform;
                node.SetParent(_poolRoot, false);
                _groupRoots[i] = node;
            }
        }

        public override void Init()
        {
            _resourceManager = Global.Get<ResourceManager>();

            _poolRoot = new GameObject($"[{nameof(InstantiateManager)}]").transform;
            _poolRoot.SetParent(GameObject.Find("[GameRoot]").transform);
            CreateGroupRoots();
        }

        public override void Update(float deltaTime)
        {
            TickIdleUnload(deltaTime);
        }

        public override void Destroy()
        {
            ClearAll();
            ClearUnpooledInstances();

            if (_poolRoot)
            {
                DestroyInstance(_poolRoot.gameObject);
                _poolRoot = null;
                _groupRoots = null;
            }

            _resourceManager = null;
        }

        #endregion

#if UNITY_EDITOR
        #region 调试

        public struct DebugGroupStat
        {
            public ResGroup Group;
            public int Active;
            public int Idle;
        }

        public struct DebugLocationStat
        {
            public string Location;
            public ResGroup Group;
            public int Lent;
            public int Idle;
            public int Pending;
            public float IdleElapsed;
            public long MemoryBytes;
        }

        public struct DebugInstanceInfo
        {
            public GameObject Instance;
            public bool Active;
        }

        /// <summary>
        /// 当前池条目数量。仅调试查询时读取。
        /// </summary>
        public int DebugEntryCount => _entries.Count;

        /// <summary>
        /// 当前不进池实例数量。仅调试查询时读取。
        /// </summary>
        public int DebugUnpooledCount => _unpooled.Count;

        /// <summary>
        /// 当前等待加载的请求数量。仅调试查询时读取。
        /// </summary>
        public int DebugPendingCount
        {
            get
            {
                int count = 0;
                foreach (SpawnEntry entry in _entries.Values)
                    count += entry.PendingCount;
                return count;
            }
        }

        /// <summary>
        /// 按分组汇总借出与空闲数量。
        /// </summary>
        public void CopyDebugGroupStats(List<DebugGroupStat> destination)
        {
            destination.Clear();
            int groupCount = (int)ResGroup.Temp + 1;
            for (int i = 0; i < groupCount; i++)
            {
                destination.Add(new DebugGroupStat
                {
                    Group = (ResGroup)i,
                    Active = 0,
                    Idle = 0
                });
            }

            foreach (SpawnEntry entry in _entries.Values)
            {
                int index = (int)entry.Group;
                DebugGroupStat stat = destination[index];
                stat.Active += entry.LentCount;
                stat.Idle += entry.Idle.Count;
                destination[index] = stat;
            }
        }

        /// <summary>
        /// 按资源位置汇总借出、空闲、等待和实例内存。
        /// </summary>
        public void CopyDebugLocationStats(List<DebugLocationStat> destination)
        {
            destination.Clear();
            var lentMemory = new Dictionary<SpawnEntry, long>();
            foreach (KeyValuePair<GameObject, InstanceEntry> pair in _lent)
            {
                if (!pair.Key)
                    continue;

                long size = UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(pair.Key);
                lentMemory.TryGetValue(pair.Value.Spawn, out long current);
                lentMemory[pair.Value.Spawn] = current + size;
            }

            foreach (KeyValuePair<string, SpawnEntry> pair in _entries)
            {
                SpawnEntry entry = pair.Value;
                lentMemory.TryGetValue(entry, out long lentBytes);
                destination.Add(new DebugLocationStat
                {
                    Location = pair.Key,
                    Group = entry.Group,
                    Lent = entry.LentCount,
                    Idle = entry.Idle.Count,
                    Pending = entry.PendingCount,
                    IdleElapsed = entry.IdleElapsed,
                    MemoryBytes = lentBytes + MeasureIdleMemory(entry)
                });
            }
        }

        /// <summary>
        /// 列出一个资源位置下的借出与空闲实例。
        /// </summary>
        public bool TryCopyDebugInstances(string location, List<DebugInstanceInfo> destination)
        {
            destination.Clear();
            if (!_entries.TryGetValue(location, out SpawnEntry entry))
                return false;

            foreach (InstanceEntry tracked in entry.Idle)
            {
                destination.Add(new DebugInstanceInfo
                {
                    Instance = tracked.Instance,
                    Active = false
                });
            }

            foreach (KeyValuePair<GameObject, InstanceEntry> pair in _lent)
            {
                if (!ReferenceEquals(pair.Value.Spawn, entry))
                    continue;

                destination.Add(new DebugInstanceInfo
                {
                    Instance = pair.Key,
                    Active = true
                });
            }

            return true;
        }

        /// <summary>
        /// 列出不进池的实例。
        /// </summary>
        public void CopyDebugUnpooled(List<GameObject> destination)
        {
            destination.Clear();
            foreach (GameObject instance in _unpooled)
                destination.Add(instance);
        }

        private static long MeasureIdleMemory(SpawnEntry entry)
        {
            long bytes = 0;
            foreach (InstanceEntry tracked in entry.Idle)
            {
                if (tracked.Instance)
                    bytes += UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(tracked.Instance);
            }

            return bytes;
        }

        #endregion
#endif
    }
}