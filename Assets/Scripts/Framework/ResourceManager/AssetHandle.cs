using System;
using System.Runtime.CompilerServices;
using Object = UnityEngine.Object;

namespace Framework
{
    /// <summary>
    /// 一次资源加载的所有权凭证；由资源策略子类化，提供引擎对象与释放语义。
    /// </summary>
    public abstract class AssetHandle<T> : IDisposable where T : Object
    {
        private const string CANCELLED_ERROR = "Load cancelled.";

        private Action _continuation;

        #region 属性
        public AssetLoadState State { get; private set; }
        public string Error { get; private set; }
        public T Asset { get; private set; }

        public bool IsDisposed { get; private set; }
        public bool IsCompleted => State != AssetLoadState.Loading;
        public bool IsValid => !IsDisposed && State == AssetLoadState.Succeeded;
        #endregion
        
        protected AssetHandle()
        {
            State = AssetLoadState.Loading;
        }

        protected AssetHandle(T asset)
        {
            State = AssetLoadState.Succeeded;
            Asset = asset;
        }

        protected AssetHandle(string error)
        {
            State = AssetLoadState.Failed;
            Error = error;
        }
        
        public AssetHandleAwaiter GetAwaiter() => new AssetHandleAwaiter(this);

        #region 资源状态控制

        protected void Complete(T asset)
        {
            if (State != AssetLoadState.Loading)
                return;

            State = AssetLoadState.Succeeded;
            Asset = asset;
            InvokeContinuation();
        }

        protected void Fail(string error)
        {
            if (State != AssetLoadState.Loading)
                return;

            State = AssetLoadState.Failed;
            Error = error;
            InvokeContinuation();
        }
        
        /// <summary>
        /// 资源释放方法，由具体实现类决定
        /// </summary>
        protected abstract void Release();

        public void Dispose()
        {
            if (IsDisposed)
                return;

            IsDisposed = true;

            if (State == AssetLoadState.Loading)
                Cancel();

            Release();
        }

        private void Cancel()
        {
            if (State != AssetLoadState.Loading)
                return;

            State = AssetLoadState.Failed;
            Error = CANCELLED_ERROR;
            InvokeContinuation();
        }

        #endregion

        private void RegisterContinuation(Action continuation)
        {
            if (IsCompleted)
                continuation();
            else
                _continuation += continuation;
        }

        private void InvokeContinuation()
        {
            var continuation = _continuation;
            _continuation = null;
            continuation?.Invoke();
        }

        /// <summary>
        /// 资源状态句柄，资源加载完毕之后调用回调
        /// </summary>
        public readonly struct AssetHandleAwaiter : INotifyCompletion
        {
            private readonly AssetHandle<T> _handle;
            internal AssetHandleAwaiter(AssetHandle<T> handle) => _handle = handle;

            public bool IsCompleted => _handle.IsCompleted;
            public AssetHandle<T> GetResult() => _handle;
            public void OnCompleted(Action continuation) => _handle.RegisterContinuation(continuation);
        }
    }
}
