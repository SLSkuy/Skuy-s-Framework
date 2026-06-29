using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Framework
{
    public abstract class BaseDataProxy : IDataProxy
    {
        public abstract string DataName { get; }
        public abstract bool IsNeedSaveToLocal { get; }
        public bool IsInitialized { get; private set; }

        private readonly HashSet<string> _loadedAssetPaths = new();
        private readonly HashSet<string> _loadedAssetPrefixes = new();

        public virtual void Init()
        {
        }

        public void _Init()
        {
            if (IsInitialized) return;

            Load();
            Init();
            IsInitialized = true;
        }

        public virtual void Load()
        {
        }

        public virtual void Save()
        {
        }

        public virtual void Clear()
        {
        }

        public void _Clear()
        {
            if (!IsInitialized) return;

            if (IsNeedSaveToLocal) Save();
            Clear();
            ReleaseLoadedResources();
            IsInitialized = false;
        }

        /// <summary>
        /// 资源加载方法，自动与资源管理器对接
        /// </summary>
        protected T LoadAsset<T>(string path) where T : Object
        {
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError($"[{GetType().Name}] LoadAsset failed: resource path is null or empty.");
                return null;
            }

            T asset = Global.Get<ResourceManager>()?.GetAsset<T>(path);
            if (asset)
            {
                _loadedAssetPaths.Add(path);
            }
            else
            {
                Debug.LogError($"[{GetType().Name}] LoadAsset failed: {path}");
            }

            return asset;
        }

        /// <summary>
        /// 资源加载方法，加载路径下的所有文件，自动与资源管理器对接
        /// </summary>
        protected IReadOnlyList<T> LoadAllAssets<T>(string path) where T : Object
        {
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogError($"[{GetType().Name}] LoadAllAssets failed: resource path is null or empty.");
                return new List<T>();
            }

            IReadOnlyList<T> assets = Global.Get<ResourceManager>()?.LoadAllAssets<T>(path);
            if (assets != null)
            {
                _loadedAssetPrefixes.Add(path);
                return assets;
            }

            Debug.LogError($"[{GetType().Name}] LoadAllAssets failed: {path}");
            return new List<T>();
        }

        /// <summary>
        /// 加载资源配置方法，自动与资源管理器对接
        /// </summary>
        /// <param name="config"></param>
        protected void LoadResourceConfig(ResourceConfig config)
        {
            if (!config)
            {
                Debug.LogError($"[{GetType().Name}] LoadResourceConfig failed: config is null.");
                return;
            }

            Global.Get<ResourceManager>()?.LoadResourceConfig(config);
            if (config.entries == null) return;

            foreach (var entry in config.entries)
            {
                if (entry == null || string.IsNullOrEmpty(entry.key)) continue;
                _loadedAssetPaths.Add(entry.key);
            }
        }

        protected GameObject Instantiate(string key, Transform parent = null, bool worldPositionStays = false)
        {
            return Global.Get<ResourceManager>()?.Instantiate(key, parent, worldPositionStays);
        }

        protected GameObject Instantiate(string key, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            return Global.Get<ResourceManager>()?.Instantiate(key, position, rotation, parent);
        }

        /// <summary>
        /// 资源销毁方法，自动处理当前数据代理中加载的资源
        /// </summary>
        private void ReleaseLoadedResources()
        {
            ResourceManager resourceManager = Global.Get<ResourceManager>();
            if (resourceManager == null)
            {
                _loadedAssetPaths.Clear();
                _loadedAssetPrefixes.Clear();
                return;
            }

            foreach (string prefix in _loadedAssetPrefixes)
            {
                resourceManager.ReleaseManagedCacheByPrefix(prefix);
            }

            foreach (string path in _loadedAssetPaths)
            {
                resourceManager.ReleaseManagedCache(path);
            }

            _loadedAssetPaths.Clear();
            _loadedAssetPrefixes.Clear();
            resourceManager.ClearUnused();
        }
    }
}
