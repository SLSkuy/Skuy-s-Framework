using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using YooAsset;

namespace Framework
{
    /// <summary>
    /// 按资源位置生成与回收 GameObject
    /// </summary>
    public sealed partial class InstantiateManager : SubSystemBase
    {
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

        #region 属性

        public override int Priority => (int)SubSystemPriority.SpawnManager;

        #endregion

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
    }
}
