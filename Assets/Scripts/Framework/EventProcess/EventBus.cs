using System;
using System.Collections.Generic;

namespace EventProcess
{
    /// <summary>
    /// 事件类
    /// </summary>
    public interface IEvent
    {
        string Hash { get; }
    }
    
    public static class EventBus
    {
        private static readonly EventHub hub = new EventHub();

        public static SType Get<SType>() where SType : IEvent, new() {
            return hub.Get<SType>();
        }
    }

    /// <summary>
    /// 事件中心
    /// </summary>
    public class EventHub
    {
        private readonly Dictionary<Type, IEvent> _signals = new Dictionary<Type, IEvent>();

        /// <summary>
        /// 根据类型获取事件
        /// </summary>
        public TSType Get<TSType>() where TSType : IEvent, new() {
            Type signalType = typeof(TSType);

            if (_signals.TryGetValue(signalType, out var @event)) {
                return (TSType) @event;
            }

            return (TSType) Bind(signalType);
        }

        /// <summary>
        /// 手动提供一个事件的哈希，并将其绑定到给定的监听器
        /// </summary> 
        public void AddListenerToHash(string signalHash, Action handler) {
            IEvent @event = GetSignalByHash(signalHash);
            if (@event != null && @event is AEvent) {
                (@event as AEvent).AddListener(handler);
            }
        }
        
        public void RemoveListenerFromHash(string signalHash, Action handler) {
            IEvent @event = GetSignalByHash(signalHash);
            if (@event != null && @event is AEvent) {
                (@event as AEvent).RemoveListener(handler);
            }
        }

        private IEvent Bind(Type signalType) {
            if (_signals.TryGetValue(signalType, out var @event)) {
                UnityEngine.Debug.LogError($"Signal already registered for type {signalType}");
                return @event;
            }

            @event = (IEvent) Activator.CreateInstance(signalType);
            _signals.Add(signalType, @event);
            return @event;
        }

        private IEvent Bind<T>() where T : IEvent, new() {
            return Bind(typeof(T));
        }

        private IEvent GetSignalByHash(string signalHash) {
            foreach (IEvent signal in _signals.Values) {
                if (signal.Hash == signalHash) {
                    return signal;
                }
            }

            return null;
        }
    }

    /// <summary>
    /// 信号事件的抽象类
    /// </summary>
    public abstract class ABaseEvent : IEvent
    {
        protected string _hash;

        /// <summary>
        /// 本身的哈希值
        /// </summary>
        public string Hash {
            get {
                if (string.IsNullOrEmpty(_hash)) {
                    _hash = this.GetType().ToString();
                }

                return _hash;
            }
        }
    }

    /// <summary>
    /// 事件的具体实现
    /// </summary>
    public abstract class AEvent : ABaseEvent
    {
        private Action callback;

        /// <summary>
        /// 事件加监听
        /// </summary>
        public void AddListener(Action handler) {
#if UNITY_EDITOR
            UnityEngine.Debug.Assert(
                handler.Method.GetCustomAttributes(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute),
                    inherit: false).Length == 0,
                "Adding anonymous delegates as Signal callbacks is not supported (you wouldn't be able to unregister them later).");
#endif
            callback += handler;
        }

        /// <summary>
        /// 事件的移除监听
        /// </summary>
        public void RemoveListener(Action handler) {
            callback -= handler;
        }

        /// <summary>
        /// 广播事件
        /// </summary>
        public void Dispatch() {
            if (callback != null) {
                callback();
            }
        }
    }

    /// <summary>
    /// 带一个参数的事件
    /// </summary>
    public abstract class AEvent<T> : ABaseEvent
    {
        private Action<T> _callback;

        /// <summary>
        /// 事件加监听
        /// </summary>
        public void AddListener(Action<T> handler) {
#if UNITY_EDITOR
            UnityEngine.Debug.Assert(
                handler.Method.GetCustomAttributes(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute),
                    inherit: false).Length == 0,
                "Adding anonymous delegates as Signal callbacks is not supported (you wouldn't be able to unregister them later).");
#endif
            _callback += handler;
        }

        /// <summary>
        /// 移除监听
        /// </summary>
        public void RemoveListener(Action<T> handler) {
            _callback -= handler;
        }

        /// <summary>
        /// 广播事件，带一个参数
        /// </summary>
        public void Dispatch(T arg1) {
            _callback?.Invoke(arg1);
        }
    }

    /// <summary>
    /// 同理，2个参数的事件
    /// </summary>
    public abstract class AEvent<T, U> : ABaseEvent
    {
        private Action<T, U> callback;
        
        public void AddListener(Action<T, U> handler) {
#if UNITY_EDITOR
            UnityEngine.Debug.Assert(
                handler.Method.GetCustomAttributes(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute),
                    inherit: false).Length == 0,
                "Adding anonymous delegates as Signal callbacks is not supported (you wouldn't be able to unregister them later).");
#endif
            callback += handler;
        }
        
        public void RemoveListener(Action<T, U> handler) {
            callback -= handler;
        }
        
        public void Dispatch(T arg1, U arg2) {
            if (callback != null) {
                callback(arg1, arg2);
            }
        }
    }

    /// <summary>
    /// 同理三个参数的事件
    /// </summary>
    public abstract class AEvent<T, U, V> : ABaseEvent
    {
        private Action<T, U, V> callback;
        
        public void AddListener(Action<T, U, V> handler) {
#if UNITY_EDITOR
            UnityEngine.Debug.Assert(
                handler.Method.GetCustomAttributes(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute),
                    inherit: false).Length == 0,
                "Adding anonymous delegates as Signal callbacks is not supported (you wouldn't be able to unregister them later).");
#endif
            callback += handler;
        }
        
        public void RemoveListener(Action<T, U, V> handler) {
            callback -= handler;
        }
        
        public void Dispatch(T arg1, U arg2, V arg3) {
            if (callback != null) {
                callback(arg1, arg2, arg3);
            }
        }
    }
}