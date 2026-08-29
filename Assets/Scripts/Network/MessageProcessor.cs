using System;
using System.Collections.Generic;
using Events;
using Google.Protobuf;
using Utils;

namespace Network
{
    /// <summary>
    /// 网络消息分发：按事件保存处理器实例列表，支持多播与按实例注销。
    /// </summary>
    public sealed class MessageProcessor
    {
        private readonly Dictionary<NetEvent, List<object>> _clientHandlers = new();
        private readonly Dictionary<NetEvent, List<object>> _serverHandlers = new();
        private readonly Dictionary<object, HandlerBinding> _bindings = new();

        /// <summary>
        /// 分发客户端消息到该事件上所有仍登记的处理器。
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
        /// 登记客户端处理器。同一实例重复登记为无操作。
        /// </summary>
        public void Register<T>(NetEvent eventId, INetHandler<T> handler) where T : class, IMessage, new()
        {
            if (handler == null) return;
            Bind(eventId, handler, server: false, (_, msg) => handler.Handle((T)msg));
            NetUtils.RegisterParser(eventId, new T().Descriptor.Parser);
        }

        /// <summary>
        /// 登记服务端处理器。同一实例重复登记为无操作。
        /// </summary>
        public void RegisterServer<T>(NetEvent eventId, IServerNetHandler<T> handler) where T : class, IMessage, new()
        {
            if (handler == null) return;
            Bind(eventId, handler, server: true, (id, msg) => handler.Handle(id, (T)msg));
            NetUtils.RegisterParser(eventId, new T().Descriptor.Parser);
        }

        /// <summary>
        /// 按处理器实例注销。未登记过的实例为无操作。
        /// </summary>
        public void Unregister(object handler)
        {
            if (handler == null) return;
            if (!_bindings.Remove(handler, out HandlerBinding binding)) return;

            Dictionary<NetEvent, List<object>> map = binding.Server ? _serverHandlers : _clientHandlers;
            if (!map.TryGetValue(binding.EventId, out List<object> list)) return;
            list.Remove(handler);
        }

        private void Bind(NetEvent eventId, object handler, bool server, Action<uint, IMessage> invoke)
        {
            if (_bindings.ContainsKey(handler)) return;

            Dictionary<NetEvent, List<object>> map = server ? _serverHandlers : _clientHandlers;
            if (!map.TryGetValue(eventId, out List<object> list))
            {
                list = new List<object>();
                map[eventId] = list;
            }

            list.Add(handler);
            _bindings[handler] = new HandlerBinding(eventId, server, invoke);
        }

        private void Dispatch(NetEvent evt, uint senderId, IMessage message, bool server)
        {
            Dictionary<NetEvent, List<object>> map = server ? _serverHandlers : _clientHandlers;
            if (!map.TryGetValue(evt, out List<object> list) || list.Count == 0) return;

            object[] snapshot = list.ToArray();
            for (int i = 0; i < snapshot.Length; i++)
            {
                object handler = snapshot[i];
                if (_bindings.TryGetValue(handler, out HandlerBinding binding))
                {
                    binding.Invoke(senderId, message);
                }
            }
        }

        private readonly struct HandlerBinding
        {
            public readonly NetEvent EventId;
            public readonly bool Server;
            public readonly Action<uint, IMessage> Invoke;

            public HandlerBinding(NetEvent eventId, bool server, Action<uint, IMessage> invoke)
            {
                EventId = eventId;
                Server = server;
                Invoke = invoke;
            }
        }
    }
}
