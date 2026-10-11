using System;
using System.Collections.Generic;
using Events;
using Google.Protobuf;
using Utils;

namespace Network
{
    /// <summary>
    /// 按事件标识分发已解码消息，同一事件可挂多条回调，按事件与回调成对注销
    /// </summary>
    public class MessageProcessor
    {
        /// <summary>
        /// 槽位：Callback 按登记时的同一委托实例注销；Invoke 在登记时闭包具体 T，分发时直接调用。
        /// </summary>
        private readonly struct CallBackBinding
        {
            public readonly Delegate Callback;
            public readonly Action<uint, IMessage> Invoke;

            public CallBackBinding(Delegate callback, Action<uint, IMessage> invoke)
            {
                Callback = callback;
                Invoke = invoke;
            }
        }
        
        private readonly Dictionary<NetEvent, List<CallBackBinding>> _clientBindings = new();
        private readonly Dictionary<NetEvent, List<CallBackBinding>> _serverBindings = new();

        /// <summary>
        /// 分发客户端消息
        /// </summary>
        /// <param name="evt">消息类型</param>
        /// <param name="message">消息包体</param>
        public void HandleMessage(NetEvent evt, IMessage message)
        {
            Dispatch(evt, 0, message, server: false);
        }

        /// <summary>
        /// 分发服务端接收到的消息
        /// </summary>
        /// <param name="senderId">发送方连接ID</param>
        /// <param name="evt">消息类型</param>
        /// <param name="message">消息包体</param>
        public void HandleServerMessage(uint senderId, NetEvent evt, IMessage message)
        {
            Dispatch(evt, senderId, message, server: true);
        }

        /// <summary>
        /// 将客户端回调挂到指定事件。同一对重复登记为无操作。
        /// </summary>
        public void Register<T>(NetEvent eventId, Action<T> callback) where T : class, IMessage, new()
        {
            if (callback == null) return;
            if (!Add(eventId, callback, server: false, (_, msg) => callback((T)msg))) return;
            NetUtils.RegisterParser<T>(eventId);
        }

        /// <summary>
        /// 将服务端回调挂到指定事件。同一对重复登记为无操作。
        /// </summary>
        public void RegisterServer<T>(NetEvent eventId, Action<uint, T> callback) where T : class, IMessage, new()
        {
            if (callback == null) return;
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
            Dictionary<NetEvent, List<CallBackBinding>> map = server ? _serverBindings : _clientBindings;
            if (!map.TryGetValue(eventId, out List<CallBackBinding> list))
            {
                list = new List<CallBackBinding>();
                map[eventId] = list;
            }

            if (IndexOf(list, callback) >= 0) return false;
            list.Add(new CallBackBinding(callback, invoke));
            return true;
        }

        private void Remove(NetEvent eventId, Delegate callback, bool server)
        {
            if (callback == null) return;
            Dictionary<NetEvent, List<CallBackBinding>> map = server ? _serverBindings : _clientBindings;
            if (!map.TryGetValue(eventId, out List<CallBackBinding> list)) return;

            int index = IndexOf(list, callback);
            if (index < 0) return;
            list.RemoveAt(index);
        }

        private void Dispatch(NetEvent evt, uint senderId, IMessage message, bool server)
        {
            Dictionary<NetEvent, List<CallBackBinding>> map = server ? _serverBindings : _clientBindings;
            if (!map.TryGetValue(evt, out List<CallBackBinding> list) || list.Count == 0) return;

            for (int i = 0; i < list.Count; i++)
            {
                list[i].Invoke(senderId, message);
            }
        }

        private static int IndexOf(List<CallBackBinding> list, Delegate callback)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i].Callback.Equals(callback)) return i;
            }

            return -1;
        }
    }
}
