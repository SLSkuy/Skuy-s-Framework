using System;
using Object = UnityEngine.Object;

namespace Framework
{
    /// <summary>
    /// 进程级资源门面：持有当前资源策略并转发加载请求，不缓存对象、不二次计数。
    /// </summary>
    public class AssetManager : SubSystemBase
    {
        private IAssetProvider _provider;
        private bool _providerInjected;
        private bool _providerShutdown;

        public override int Priority => (int)SubSystemPriority.ResourceManager;

        #region 属性
        private IAssetProvider ActiveProvider
        {
            get
            {
                if (!_providerInjected)
                    throw new InvalidOperationException("Asset provider must be injected before use.");

                if (_providerShutdown)
                    throw new InvalidOperationException("Asset manager has been shut down.");

                return _provider;
            }
        }
        #endregion

        /// <summary>
        /// 注入资源策略，只能调用一次。
        /// </summary>
        public void Init(IAssetProvider provider)
        {
            if (_providerInjected)
                throw new InvalidOperationException("Asset provider has already been injected.");

            if (provider == null)
                throw new ArgumentNullException(nameof(provider));

            _provider = provider;
            _providerInjected = true;
        }

        /// <summary>
        /// 按资源位置同步加载，返回终态句柄。
        /// </summary>
        public AssetHandle<T> Load<T>(AssetLocation location) where T : Object
        {
            ValidateLocation(location);
            return ActiveProvider.Load<T>(location);
        }

        /// <summary>
        /// 按资源位置异步加载，立即返回加载中句柄。
        /// </summary>
        public AssetHandle<T> LoadAsync<T>(AssetLocation location) where T : Object
        {
            ValidateLocation(location);
            return ActiveProvider.LoadAsync<T>(location);
        }

        /// <summary>
        /// 卸未使用资源，转发给当前资源策略。
        /// </summary>
        public void UnloadUnused() => ActiveProvider.UnloadUnused();

        public override void Destroy()
        {
            if (_providerInjected && !_providerShutdown)
            {
                _provider.Shutdown();
                _providerShutdown = true;
            }
        }

        private void ValidateLocation(AssetLocation location)
        {
            if (string.IsNullOrEmpty(location.Location))
                throw new ArgumentException($"[{GetType()}] Asset location is required.", nameof(location));
        }
    }
}
