using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Events;
using Google.Protobuf;
using Utils;

namespace Network
{
    /// <summary>
    /// 按事件标识分发已解码消息。同一事件可挂多条回调，按事件与回调成对注销。
    /// </summary>
    public sealed class MessageProcessor
    {
        private readonly Dictionary<NetEvent, List<Binding>> _clientBindings = new();
        private readonly Dictionary<NetEvent, List<Binding>> _serverBindings = new();

        /// <summary>
        /// 分发客户端消息。
        /// </summary>
        public void HandleMessage(NetEvent evt, IMessage message)
        {
            Dispatch(evt, 0, message, server: false);
        }

        /// <summary>
        /// 分发服务端消息（含发送方连接标识）。
        /// </summary>
        public void HandleServerMessage(uint clientId, NetEvent evt, IMessage message)
        {
            Dispatch(evt, clientId, message, server: true);
        }

        /// <summary>
        /// 将客户端回调挂到指定事件。同一对重复登记为无操作。
        /// </summary>
        public void Register<T>(NetEvent eventId, Action<T> callback) where T : class, IMessage, new()
        {
            if (callback == null) return;
            AssertNotAnonymous(callback);
            if (!Add(eventId, callback, server: false, (_, msg) => callback((T)msg))) return;
            NetUtils.RegisterParser<T>(eventId);
        }

        /// <summary>
        /// 将服务端回调挂到指定事件。同一对重复登记为无操作。
        /// </summary>
        public void RegisterServer<T>(NetEvent eventId, Action<uint, T> callback) where T : class, IMessage, new()
        {
            if (callback == null) return;
            AssertNotAnonymous(callback);
            if (!Add(eventId, callback, server: true, (id, msg) => callback(id, (T)msg))) return;
            NetUtils.RegisterParser<T>(eventId);
        }

        /// <summary>
        /// 从指定事件拆除客户端回调。未登记为无操作。
        /// </summary>
        public void Unregister<T>(NetEvent eventId, Action<T> callback)
        {
            Remove(eventId, callback, server: false);
        }

        /// <summary>
        /// 从指定事件拆除服务端回调。未登记为无操作。
        /// </summary>
        public void UnregisterServer<T>(NetEvent eventId, Action<uint, T> callback)
        {
            Remove(eventId, callback, server: true);
        }

        private bool Add(NetEvent eventId, Delegate callback, bool server, Action<uint, IMessage> invoke)
        {
            Dictionary<NetEvent, List<Binding>> map = server ? _serverBindings : _clientBindings;
            if (!map.TryGetValue(eventId, out List<Binding> list))
            {
                list = new List<Binding>();
                map[eventId] = list;
            }

            if (IndexOf(list, callback) >= 0) return false;
            list.Add(new Binding(callback, invoke));
            return true;
        }

        private void Remove(NetEvent eventId, Delegate callback, bool server)
        {
            if (callback == null) return;
            Dictionary<NetEvent, List<Binding>> map = server ? _serverBindings : _clientBindings;
            if (!map.TryGetValue(eventId, out List<Binding> list)) return;

            int index = IndexOf(list, callback);
            if (index < 0) return;
            list.RemoveAt(index);
        }

        private void Dispatch(NetEvent evt, uint senderId, IMessage message, bool server)
        {
            Dictionary<NetEvent, List<Binding>> map = server ? _serverBindings : _clientBindings;
            if (!map.TryGetValue(evt, out List<Binding> list) || list.Count == 0) return;

            for (int i = 0; i < list.Count; i++)
            {
                list[i].Invoke(senderId, message);
            }
        }

        private static int IndexOf(List<Binding> list, Delegate callback)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Callback.Equals(callback)) return i;
            }

            return -1;
        }

        private static void AssertNotAnonymous(Delegate callback)
        {
#if UNITY_EDITOR
            UnityEngine.Debug.Assert(
                callback.Method.GetCustomAttributes(typeof(CompilerGeneratedAttribute), inherit: false).Length == 0,
                "Adding anonymous delegates as network callbacks is not supported (you wouldn't be able to unregister them later).");
#endif
        }

        /// <summary>
        /// 槽位：Callback 用于按方法组注销；Invoke 在登记时闭包具体 T，分发时直接调用。
        /// </summary>
        private readonly struct Binding
        {
            public readonly Delegate Callback;
            public readonly Action<uint, IMessage> Invoke;

            public Binding(Delegate callback, Action<uint, IMessage> invoke)
            {
                Callback = callback;
                Invoke = invoke;
            }
        }
    }
}
