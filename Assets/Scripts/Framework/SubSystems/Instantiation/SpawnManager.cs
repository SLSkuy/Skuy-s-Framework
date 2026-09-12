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
    /// 按资源位置生成与回收 GameObject，内部池化
    /// </summary>
    public sealed class SpawnManager : SubSystemBase
    {
        private ResourceManager _resourceManager;
        private Transform _poolRoot;
        private readonly Dictionary<string, AssetHandle> _handles = new();
        private readonly Dictionary<string, Queue<GameObject>> _idle = new();
        private readonly Dictionary<GameObject, string> _lent = new();
        private readonly Dictionary<string, float> _idleElapsed = new();
        private readonly List<PendingInstantiate> _pending = new();
        private readonly List<string> _idleUnloadScratch = new();
        private float _idleUnloadSeconds;

        private sealed class PendingInstantiate
        {
            public string location;
            public InstantiateOptions options;
            public readonly TaskCompletionSource<GameObject> completion = new(TaskCreationOptions.RunContinuationsAsynchronously);
            public AssetHandle handle;
            public bool failed;
            public bool seenUpdate;
        }

        #region 属性
        public override int Priority => (int)SubSystemPriority.InstantiationManager;
        #endregion

        /// <summary>
        /// 按资源位置同步生成实例。
        /// </summary>
        public GameObject Instantiate(string location, InstantiateOptions options = default)
        {
            return InstantiateFromLocation(location, options, 
                instance => instance.transform.SetParent(null));
        }

        /// <summary>
        /// 按资源位置异步生成实例。池命中当帧完成；需要加载时至少等到下一次子系统 Update。
        /// </summary>
        public Task<GameObject> InstantiateAsync(string location, InstantiateOptions options = default)
        {
            return InstantiateAsyncFromLocation(location, default,
                instance => instance.transform.SetParent(null));
        }

        /// <summary>
        /// 将实例门面生成的对象还池。外源、空引用或重复 Release 会打错误日志并忽略。
        /// </summary>
        public void Release(GameObject instance)
        {
            if (!instance)
            {
                Debug.LogError("[InstantiationManager] Release ignored: instance is null.");
                return;
            }

            if (!_lent.Remove(instance, out string location))
            {
                Debug.LogError("[InstantiationManager] Release ignored: instance was not created by the instance facade.");
                return;
            }

            instance.SetActive(false);
            instance.transform.SetParent(_poolRoot);
            if (!_idle.TryGetValue(location, out Queue<GameObject> idle))
            {
                idle = new Queue<GameObject>();
                _idle[location] = idle;
            }

            idle.Enqueue(instance);
            if (!IsLocationBusy(location) && !_idleElapsed.ContainsKey(location))
                _idleElapsed[location] = 0f;
        }

        /// <summary>
        /// 销毁全部借出与空闲实例，并释放预制体句柄。切场景与子系统销毁走这条清空。
        /// </summary>
        public void Clear()
        {
            for (int i = 0; i < _pending.Count; i++)
            {
                PendingInstantiate pending = _pending[i];
                if (pending.handle != null && !_handles.ContainsValue(pending.handle))
                    pending.handle.Dispose();
                pending.completion.TrySetResult(null);
            }

            _pending.Clear();

            foreach (GameObject instance in _lent.Keys)
                DestroyInstance(instance);

            _lent.Clear();

            foreach (Queue<GameObject> idle in _idle.Values)
            {
                while (idle.Count > 0)
                    DestroyInstance(idle.Dequeue());
            }

            _idle.Clear();

            foreach (AssetHandle handle in _handles.Values)
                handle.Dispose();

            _handles.Clear();
            _idleElapsed.Clear();
        }

        /// <summary>
        /// 同步执行预制体实例化操作
        /// </summary>
        private GameObject InstantiateFromLocation(string location, InstantiateOptions options,
            Action<GameObject> activateIdle)
        {
            if (string.IsNullOrEmpty(location))
            {
                Debug.LogError("[InstantiationManager] Instantiate failed: asset location is empty.");
                return null;
            }

            if (TryDequeueIdle(location, out GameObject pooled))
            {
                activateIdle(pooled);
                pooled.SetActive(true);
                _lent[pooled] = location;
                StopIdleTimer(location);
                return pooled;
            }

            AssetHandle handle = LoadPrefabHandle(location, false);
            if (handle == null)
                return null;

            GameObject instance = handle.InstantiateSync(options);
            if (!instance)
            {
                Debug.LogError($"[InstantiationManager] Instantiate failed: '{location}'.");
                return null;
            }

            _lent[instance] = location;
            StopIdleTimer(location);
            return instance;
        }

        
        /// <summary>
        /// 异步执行预制体实例化操作
        /// </summary>
        private Task<GameObject> InstantiateAsyncFromLocation(string location, InstantiateOptions options,
            Action<GameObject> activateIdle)
        {
            if (string.IsNullOrEmpty(location))
            {
                PendingInstantiate empty = new()
                {
                    location = location,
                    failed = true,
                };
                _pending.Add(empty);
                return empty.completion.Task;
            }

            if (TryDequeueIdle(location, out GameObject pooled))
            {
                activateIdle(pooled);
                pooled.SetActive(true);
                _lent[pooled] = location;
                StopIdleTimer(location);
                return Task.FromResult(pooled);
            }

            PendingInstantiate pending = new()
            {
                location = location,
                options = options,
            };

            AssetHandle handle = LoadPrefabHandle(location, true);
            if (handle == null)
            {
                pending.failed = true;
            }
            else
            {
                if (!handle.IsDone)
                    pending.handle = handle;
                StopIdleTimer(location);
            }

            _pending.Add(pending);
            return pending.completion.Task;
        }

        private bool TryCompletePending(PendingInstantiate pending)
        {
            if (pending.failed || string.IsNullOrEmpty(pending.location))
            {
                LogInstantiateFailed(pending.location);
                pending.completion.TrySetResult(null);
                return true;
            }

            AssetHandle handle = pending.handle ?? LoadPrefabHandle(pending.location, true);
            if (handle == null)
            {
                LogInstantiateFailed(pending.location);
                pending.completion.TrySetResult(null);
                return true;
            }

            if (!handle.IsDone)
            {
                pending.handle = handle;
                return false;
            }

            if (handle.AssetObject is not GameObject)
            {
                LogInstantiateFailed(pending.location);
                if (!_handles.ContainsValue(handle))
                    handle.Dispose();
                pending.completion.TrySetResult(null);
                return true;
            }

            _handles[pending.location] = handle;

            GameObject instance = handle.InstantiateSync(pending.options);
            if (!instance)
            {
                LogInstantiateFailed(pending.location);
                pending.completion.TrySetResult(null);
                return true;
            }

            _lent[instance] = pending.location;
            StopIdleTimer(pending.location);
            pending.completion.TrySetResult(instance);
            return true;
        }

        private static void LogInstantiateFailed(string location)
        {
            if (string.IsNullOrEmpty(location))
                Debug.LogError("[InstantiationManager] Instantiate failed: asset location is empty.");
            else
                Debug.LogError($"[InstantiationManager] Instantiate failed: '{location}'.");
        }

        private bool TryDequeueIdle(string location, out GameObject instance)
        {
            instance = null;
            if (!_idle.TryGetValue(location, out Queue<GameObject> idle))
                return false;

            while (idle.Count > 0)
            {
                instance = idle.Dequeue();
                if (instance)
                    return true;
                instance = null;
            }

            return false;
        }

        private void StopIdleTimer(string location)
        {
            _idleElapsed.Remove(location);
        }

        private bool IsLocationBusy(string location)
        {
            foreach (string lentLocation in _lent.Values)
            {
                if (lentLocation == location)
                    return true;
            }

            for (int i = 0; i < _pending.Count; i++)
            {
                PendingInstantiate pending = _pending[i];
                if (!pending.failed && pending.location == location)
                    return true;
            }

            return false;
        }

        private void TickIdleUnload(float deltaTime)
        {
            _idleUnloadScratch.Clear();
            foreach (string location in _handles.Keys)
                _idleUnloadScratch.Add(location);

            for (int i = 0; i < _idleUnloadScratch.Count; i++)
            {
                string location = _idleUnloadScratch[i];
                if (IsLocationBusy(location))
                {
                    StopIdleTimer(location);
                    continue;
                }

                _idleElapsed.TryGetValue(location, out float elapsed);
                elapsed += deltaTime;
                if (elapsed < _idleUnloadSeconds)
                {
                    _idleElapsed[location] = elapsed;
                    continue;
                }

                UnloadIdleLocation(location);
            }

            _idleUnloadScratch.Clear();
        }

        private void UnloadIdleLocation(string location)
        {
            if (_idle.TryGetValue(location, out Queue<GameObject> idle))
            {
                while (idle.Count > 0)
                    DestroyInstance(idle.Dequeue());
                _idle.Remove(location);
            }

            _handles[location].Dispose();
            _handles.Remove(location);
            StopIdleTimer(location);
        }

        private AssetHandle LoadPrefabHandle(string location, bool async)
        {
            if (_handles.TryGetValue(location, out AssetHandle cached))
                return cached;

            AssetHandle handle = async
                ? _resourceManager.LoadAsync<GameObject>(location)
                : _resourceManager.Load<GameObject>(location);

            if (handle == null || !handle.IsValid)
            {
                if (!async)
                    LogInstantiateFailed(location);
                return null;
            }

            if (!handle.IsDone)
                return handle;

            if (handle.AssetObject is not GameObject)
            {
                if (!async)
                    LogInstantiateFailed(location);
                handle.Dispose();
                return null;
            }

            _handles[location] = handle;
            return handle;
        }

        private static void DestroyInstance(GameObject instance)
        {
            if (!instance)
                return;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Object.DestroyImmediate(instance);
                return;
            }
#endif
            Object.Destroy(instance);
        }

        #region 子系统生命周期
        public override void Init()
        {
            _resourceManager = Global.Get<ResourceManager>();
            _poolRoot = new GameObject("[InstantiationManager]").transform;
            _poolRoot.SetParent(GameObject.Find("[GameRoot]").transform);

            // 初始化资源清除缓存
            _idleUnloadSeconds = GameCoreConfig.Instance.unLoadIdleTime;
        }

        public override void Update(float deltaTime)
        {
            int submitted = _pending.Count;
            for (int i = 0; i < submitted; i++)
                _pending[i].seenUpdate = true;

            for (int i = 0; i < _pending.Count;)
            {
                PendingInstantiate pending = _pending[i];
                if (!pending.seenUpdate || !TryCompletePending(pending))
                {
                    i++;
                    continue;
                }

                _pending.RemoveAt(i);
            }

            TickIdleUnload(deltaTime);
        }

        public override void Destroy()
        {
            Clear();
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
