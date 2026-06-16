using System;

namespace Framework.DataProxySystem
{
    /// <summary>
    /// 可观察值，仅支持读取和事件操作，不支持公开Set
    /// </summary>
    public interface IReadOnlyObservableValue<T>
    {
        T Value { get; }              // 只读，没有 set
        
        /// <summary>
        /// 立即推送当前值并订阅更新事件
        /// </summary>
        void PushCurrentValueAndSubscribeUpdate(Action<T> handler);
        
        /// <summary>
        /// 订阅更新事件
        /// </summary>
        void SubscribeUpdate(Action<T> handler);
        
        /// <summary>
        /// 取消订阅更新事件
        /// </summary>
        void UnsubscribeUpdate(Action<T> handler);
    }
}