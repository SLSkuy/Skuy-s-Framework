using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Framework
{
    /// <summary>
    /// 通过ResourceManager.Load 方法返回，销毁时需调用 Dispose 方法
    /// 建议使用ResourceManager.GetAsset 或者 Instantiate 方法，自动处理资源缓存
    /// </summary>
    public sealed class ResourceHandle<T> : IDisposable where T : Object
    {
        public string Key { get; }
        public T Asset { get; }
        public bool IsDisposed { get; private set; }

        private readonly ResourceManager _manager;

        public ResourceHandle(string key, T asset, ResourceManager manager)
        {
            Key = key;
            Asset = asset;
            _manager = manager;
        }

#if UNITY_EDITOR
        ~ResourceHandle()
        {
            if (IsDisposed) return;

            Debug.LogError($"[ResourceHandle] Resource handle was not disposed. Key: {Key}");
            Dispose();
        }
#endif

        public void Dispose()
        {
            if (IsDisposed) return;

            IsDisposed = true;
            _manager.Release(Key);
        }
    }
}
