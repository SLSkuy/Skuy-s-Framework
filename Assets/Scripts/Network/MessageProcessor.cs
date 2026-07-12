using System;
using System.Collections.Generic;
using Google.Protobuf;
using UnityEngine;

namespace Network
{
    /// <summary>
    /// 网络消息分发层，根据消息数据由对应的Handler进行处理
    /// </summary>
    public class MessageProcessor
    {
        private readonly Dictionary<NetEvent, Action<IMessage>> _handlers = new();
        private readonly Dictionary<NetEvent, Action<uint, IMessage>> _serverHandlers = new();

        /// <summary>
        /// 判断接收道德消息类型，并进行分发
        /// </summary>
        public void HandleMessage(NetEvent evt, IMessage message)
        {
            if (_handlers.TryGetValue(evt, out var handler))
            {
                handler(message);
            }
            else
            {
                Debug.LogWarning($"[MessageProcessor] 事件 {evt} 没有对应的处理器");
            }
        }

        /// <summary>
        /// 处理服务端消息事件
        /// </summary>
        public void HandleServerMessage(uint clientId, NetEvent evt, IMessage message)
        {
            if (_serverHandlers.TryGetValue(evt, out var handler))
            {
                handler(clientId, message);
            }
            else
            {
                Debug.LogWarning($"[MessageProcessor] 事件 {evt} 没有对应的处理器");
            }
        }
        
        /// <summary>
        /// 注册Protobuf事件处理器
        /// </summary>
        /// <param name="clientEventId">事件ID</param>
        /// <param name="handler">事件</param>
        /// <typeparam name="T">Protobuf事件类型</typeparam>
        public void Register<T>(NetEvent clientEventId, Action<T> handler) where T : IMessage, new()
        {
            _handlers[clientEventId] = msg => handler((T)msg);
            NetUtils.RegisterParser(clientEventId, new T().Descriptor.Parser);
        }

        /// <summary>
        /// 注销所有Protobuf事件处理器
        /// </summary>
        /// <param name="clientEventId">事件ID</param>
        public void UnRegister(NetEvent clientEventId)
        {
            _handlers.Remove(clientEventId);
            Debug.Log($"[MessageProcessor] 注销客户端事件 {clientEventId}");
        }
        
        /// <summary>
        /// 注册Protobuf事件处理器
        /// </summary>
        /// <param name="eventId">事件ID</param>
        /// <param name="handler">事件</param>
        /// <typeparam name="T">Protobuf事件类型</typeparam>
        public void RegisterServer<T>(NetEvent eventId, Action<uint, T> handler) where T : IMessage, new()
        {
            _serverHandlers[eventId] = (id, msg) => handler(id, (T)msg);
            NetUtils.RegisterParser(eventId, new T().Descriptor.Parser);
        }

        /// <summary>
        /// 注销所有Protobuf事件处理器
        /// </summary>
        /// <param name="eventId">事件ID</param>
        public void UnRegisterServer(NetEvent eventId)
        {
            _serverHandlers.Remove(eventId);
            Debug.Log($"[MessageProcessor] 注销服务端事件 {eventId}");
        }
    }
}
