using System;
using YooAsset;
using Object = UnityEngine.Object;

namespace Framework
{
    /// <summary>
    /// 进程级资源门面：持有当前资源策略并转发加载请求，不缓存对象、不二次计数。
    /// </summary>
    public class ResourceManager : SubSystemBase
    {
        private IResourceLoader _provider;
        private bool _providerInjected;
        private bool _providerShutdown;

        public override int Priority => (int)SubSystemPriority.ResourceManager;

        #region 属性
        private IResourceLoader ActiveProvider
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

        #region 事件
        /// <summary>
        /// 资源系统准备完毕
        /// </summary>
        public event Action OnResourceReady;
        #endregion

        /// <summary>
        /// 注入资源策略，只能调用一次。
        /// </summary>
        public void SetProvider(IResourceLoader provider)
        {
            if (_providerInjected)
                throw new InvalidOperationException("Asset provider has already been injected.");

            if (provider == null)
                throw new ArgumentNullException(nameof(provider));

            
            _provider = provider;
            _provider.InitCompleted += () => OnResourceReady?.Invoke();
            _providerInjected = true;
            
            provider.Init();
        }

        /// <summary>
        /// 按资源位置同步加载，返回 YooAsset 资源句柄。
        /// </summary>
        public AssetHandle Load<T>(string location) where T : Object
        {
            return ActiveProvider.Load<T>(location);
        }

        /// <summary>
        /// 按资源位置异步加载，返回 YooAsset 资源句柄。
        /// </summary>
        public AssetHandle LoadAsync<T>(string location) where T : Object
        {
            return ActiveProvider.LoadAsync<T>(location);
        }

        /// <summary>
        /// 卸未使用资源，转发给当前资源策略。
        /// </summary>
        public void ClearUnused() => ActiveProvider.ClearUnused();

        /// <summary>
        /// 清空所有资源
        /// </summary>
        public void ClearAll() => ActiveProvider.ClearAll();

        public override void Destroy()
        {
            if (_providerInjected && !_providerShutdown)
            {
                _provider.Shutdown();
                _providerShutdown = true;
            }
        }
    }
}
