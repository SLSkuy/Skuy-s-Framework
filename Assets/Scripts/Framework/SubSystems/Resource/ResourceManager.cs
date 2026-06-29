using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Framework
{
    public sealed class ResourceManager : SubSystemBase
    {
        public override SubSystemPriority Priority => SubSystemPriority.ResourceManager;
        
        /// <summary>
        /// 资源映射表
        /// </summary>
        private readonly Dictionary<string, ResourceInfo> _cache = new();
        
        /// <summary>
        /// 持久化数据
        /// </summary>
        private readonly HashSet<string> _persistentKeys = new();
        
        /// <summary>
        /// 正在异步加载中的资源
        /// 防止重复加载
        /// </summary>
        private readonly Dictionary<string, Task<Object>> _loadingTasks = new();

        /// <summary>
        /// 资源加载器
        /// </summary>
        private IResourceLoader _loader;

        private bool CanUnload(ResourceInfo info)
        {
            return !info.Persistent && !info.ManagedCache && info.RefCount <= 0 && info.InstanceCount <= 0;
        }

        private ResourceInfo GetOrLoadInfo<T>(string key, bool retainReference) where T : Object
        {
            if (_loader == null)
            {
                Debug.LogError("ResourceLoader is null");
                return null;
            }

            if (_cache.TryGetValue(key, out var info))
            {
                if (retainReference) info.RefCount++;
                return info;
            }

            T asset = _loader.Load<T>(key);
            if (!asset)
            {
                Debug.LogError($"Load Resource Failed : {key}");
                return null;
            }

            bool persistent = _persistentKeys.Contains(key);
            info = new ResourceInfo(asset, persistent, retainReference ? 1 : 0);
            _cache.Add(key, info);
            return info;
        }

        /// <summary>
        /// 初始化资源加载器
        /// </summary>
        /// <param name="loader"></param>
        public void InitLoader(IResourceLoader loader)
        {
            _loader = loader;
        }

        /// <summary>
        /// 从配置中加载资源
        /// </summary>
        /// <param name="config"></param>
        public void LoadResourceConfig(ResourceConfig config)
        {
            if (!config)
            {
                Debug.LogError("[ResourceManager] ResourceConfig is null");
                return;
            }

            foreach (var entry in config.entries)
            {
                if (!entry.persistent) continue;
                _persistentKeys.Add(entry.key);
                Preload<Object>(entry.key);
            }
        }
        
        public bool IsLoaded(string key)
        {
            return _cache.ContainsKey(key);
        }

        public int GetRefCount(string key)
        {
            if (_cache.TryGetValue(key, out var info))
            {
                return info.RefCount;
            }

            return 0;
        }

        #region 同步加载

        /// <summary>
        /// 同步加载资源，返回资源接收器
        /// </summary>
        /// <param name="key">资源名称</param>
        /// <typeparam name="T">资源类型</typeparam>
        public ResourceHandle<T> Load<T>(string key) where T : Object
        {
            ResourceInfo info = GetOrLoadInfo<T>(key, true);
            return info == null ? null : new ResourceHandle<T>(key, info.Asset as T, this);
        }

        /// <summary>
        /// 获取资源。调用方不需要释放，资源由 ResourceManager 缓存和清理。
        /// </summary>
        public T GetAsset<T>(string key) where T : Object
        {
            ResourceInfo info = GetOrLoadInfo<T>(key, false);
            if (info != null) info.ManagedCache = true;
            return info?.Asset as T;
        }

        /// <summary>
        /// 预加载资源并标记为缓存项，不增加外部引用。
        /// </summary>
        public void Preload<T>(string key) where T : Object
        {
            GetOrLoadInfo<T>(key, false);
        }

        /// <summary>
        /// 释放由 GetAsset/LoadAllAssets 保留的托管缓存标记。
        /// </summary>
        public void ReleaseManagedCache(string key)
        {
            if (!_cache.TryGetValue(key, out var info)) return;

            info.ManagedCache = false;
            if (!CanUnload(info)) return;

            DoUnload(key, info);
        }

        /// <summary>
        /// 按路径前缀释放托管缓存，适合关卡、UI 分组卸载
        /// </summary>
        public void ReleaseManagedCacheByPrefix(string keyPrefix)
        {
            List<string> keys = new();
            foreach (var pair in _cache)
            {
                if (!pair.Key.StartsWith(keyPrefix, StringComparison.Ordinal)) continue;
                keys.Add(pair.Key);
            }

            foreach (string key in keys)
            {
                ReleaseManagedCache(key);
            }
        }

        /// <summary>
        /// 从目录批量加载资源，适合配置表、ScriptableObject 数据目录
        /// </summary>
        public IReadOnlyList<T> LoadAllAssets<T>(string key) where T : Object
        {
            if (_loader == null)
            {
                Debug.LogError("ResourceLoader is null");
                return Array.Empty<T>();
            }

            T[] assets = _loader.LoadAll<T>(key);
            if (assets == null || assets.Length == 0) return Array.Empty<T>();

            foreach (T asset in assets)
            {
                if (!asset) continue;

                string assetKey = $"{key}/{asset.name}";
                if (!_cache.ContainsKey(assetKey))
                {
                    bool persistent = _persistentKeys.Contains(assetKey);
                    _cache.Add(assetKey, new ResourceInfo(asset, persistent) { ManagedCache = true });
                }
                else
                {
                    // 防止前置通过Load加载未设置ManagedCache，防止错误的销毁
                    _cache[assetKey].ManagedCache = true;
                }
            }

            return assets;
        }

        /// <summary>
        /// 实例化托管 Prefab。实例销毁时会自动释放 Prefab 占用。
        /// </summary>
        public GameObject Instantiate(string key, Transform parent = null, bool worldPositionStays = false)
        {
            ResourceInfo info = GetOrLoadInfo<GameObject>(key, false);
            GameObject prefab = info?.Asset as GameObject;
            if (!prefab) return null;

            GameObject instance = Object.Instantiate(prefab, parent, worldPositionStays);
            TrackInstance(key, instance);
            return instance;
        }

        public GameObject Instantiate(string key, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            ResourceInfo info = GetOrLoadInfo<GameObject>(key, false);
            GameObject prefab = info?.Asset as GameObject;
            if (!prefab) return null;

            GameObject instance = Object.Instantiate(prefab, position, rotation, parent);
            TrackInstance(key, instance);
            return instance;
        }

        private void TrackInstance(string key, GameObject instance)
        {
            if (!instance) return;
            if (!_cache.TryGetValue(key, out var info)) return;

            info.InstanceCount++;
            ResourceInstanceTracker tracker = instance.GetComponent<ResourceInstanceTracker>();
            if (!tracker) tracker = instance.AddComponent<ResourceInstanceTracker>();
            tracker.Init(this, key);
        }

        #endregion

        #region 异步加载

        /// <summary>
        /// 异步加载资源
        /// </summary>
        /// <param name="key">资源名称</param>
        /// <typeparam name="T">资源类型</typeparam>
        public async Task<ResourceHandle<T>> LoadAsync<T>(string key) where T : Object
        {
            if (_loader == null)
            {
                Debug.LogError("ResourceLoader is null");
                return null;
            }

            // 已缓存
            if (_cache.TryGetValue(key, out var info))
            {
                info.RefCount++;
                return new ResourceHandle<T>(key, info.Asset as T, this);
            }

            // 正在加载
            if (_loadingTasks.TryGetValue(key, out var loadingTask))
            {
                Object loadingAsset = await loadingTask;
                if (!loadingAsset) return null;
                if (_cache.TryGetValue(key, out var loadedInfo))
                {
                    loadedInfo.RefCount++;
                }
                return new ResourceHandle<T>(key, loadingAsset as T, this);
            }

            Task<Object> task = DoLoadAsync<T>(key);
            _loadingTasks.Add(key, task);
            try
            {
                Object result = await task;
                if(!result) return null;
                return new ResourceHandle<T>(key, result as T, this);
            }
            finally
            {
                _loadingTasks.Remove(key);
            }
        }
        
        private async Task<Object> DoLoadAsync<T>(string key) where T : Object
        {
            T asset = await _loader.LoadAsync<T>(key);
            if (!asset)
            {
                Debug.LogError($"LoadAsync Resource Failed : {key}");
                return null;
            }

            bool persistent = _persistentKeys.Contains(key);
            _cache.Add(key, new ResourceInfo(asset, persistent, 1));

            return asset;
        }

        #endregion

        #region 资源释放

        /// <summary>
        /// 释放资源
        /// </summary>
        /// <param name="key">资源名称</param>
        public void Release(string key)
        {
            if (!_cache.TryGetValue(key, out var info))
            {
                Debug.LogWarning($"Release Failed, Resource Not Found : {key}");
                return;
            }

            info.RefCount--;
            if (info.RefCount < 0)
            {
                Debug.LogError($"Resource RefCount < 0 : {key}");
                info.RefCount = 0;
            }

            if (info.RefCount > 0) return;
            if (!CanUnload(info)) return;

            DoUnload(key, info);
        }

        internal void ReleaseInstance(string key)
        {
            if (!_cache.TryGetValue(key, out var info)) return;

            info.InstanceCount--;
            if (info.InstanceCount < 0)
            {
                Debug.LogError($"Resource InstanceCount < 0 : {key}");
                info.InstanceCount = 0;
            }

            if (!CanUnload(info)) return;

            DoUnload(key, info);
        }
        
        private void DoUnload(string key, ResourceInfo info)
        {
            Debug.Log($"[ResourceManager] Unload Resource : {key}");
            _loader.Unload(info.Asset);
            _cache.Remove(key);
        }
        
        /// <summary>
        /// 清理未使用资源
        /// </summary>
        public void ClearUnused(bool force = false)
        {
            List<string> removeKeys = new();

            foreach (var pair in _cache)
            {
                ResourceInfo info = pair.Value;
                if (!CanUnload(info)) continue;
                _loader.Unload(info.Asset);
                removeKeys.Add(pair.Key);
            }

            foreach (string key in removeKeys)
            {
                _cache.Remove(key);
            }

            Resources.UnloadUnusedAssets();
            if(force) GC.Collect();
        }

        /// <summary>
        /// 强制清空所有资源
        /// </summary>
        public void ForceClearAll()
        {
            foreach (var pair in _cache)
            {
                _loader.Unload(pair.Value.Asset);
            }
            _cache.Clear();
            _persistentKeys.Clear();
            Resources.UnloadUnusedAssets();
            GC.Collect();
        }

        #endregion

        #region 生命周期

        public override void Init()
        {
            // 后续根据需要切换资源加载器
            InitLoader(new ResourcesLoader());
        }

        public override void Destroy()
        {
            ForceClearAll();
            _loadingTasks.Clear();
            _loader = null;
        }

        #endregion
    }
}
