using System;
using System.Collections.Generic;

namespace Framework.DataProxySystem
{
    /// <summary>
    /// 可观察值，完整读写实现。对外暴露 IReadOnlyObservableValue 接口可阻止外部写入
    /// </summary>
    public class ObservableValue<T> : IReadOnlyObservableValue<T>
    {
        public ObservableValue(T value = default)
        {
            _value = value;
        }

        private event Action<T> OnValueChanged;
        private T _value;

        /// <summary>
        /// 公开 get/set，外部可正常写入
        /// </summary>
        public T Value
        {
            get => _value;
            set => Set(value);
        }

        private void Set(T newValue)
        {
            if (EqualityComparer<T>.Default.Equals(_value, newValue)) return;
            _value = newValue;
            OnValueChanged?.Invoke(newValue);
        }
        
        /// <summary>
        /// 立即推送当前值并订阅更新事件
        /// </summary>
        public void PushCurrentValueAndSubscribeUpdate(Action<T> handler)
        {
            OnValueChanged += handler;
            handler(_value);
        }
        
        /// <summary>
        /// 订阅更新事件
        /// </summary>
        public void SubscribeUpdate(Action<T> handler)
        {
            OnValueChanged += handler;
        }
        
        /// <summary>
        /// 取消订阅更新事件
        /// </summary>
        public void UnsubscribeUpdate(Action<T> handler)
        {
            OnValueChanged -= handler;
        }
    }
}