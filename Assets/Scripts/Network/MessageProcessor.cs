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

        /// <summary>
        /// 判断接收道德消息类型，并进行分发
        /// </summary>
        /// <param name="message"></param>
        public void HandleMessage(IMessage message)
        {
            NetEvent eventId = NetEvent.ERROR;
            
            // TODO: 解析Protobuf消息类型
            
            if (_handlers.TryGetValue(eventId, out var handler))
            {
                handler(message);
            }
            else
            {
                Debug.LogWarning($"[MessageProcessor] 事件 {eventId} 没有对应的处理器");
            }
        }
        
        /// <summary>
        /// 注册Protobuf事件处理器
        /// </summary>
        /// <param name="eventId">事件ID</param>
        /// <param name="handler">事件</param>
        /// <typeparam name="T">Protobuf事件类型</typeparam>
        public void Register<T>(NetEvent eventId, Action<T> handler) where T : IMessage, new()
        {
            _handlers[eventId] = msg => handler((T)msg);
            Debug.Log($"[MessageProcessor] 注册事件 {eventId}");
        }

        /// <summary>
        /// 注销所有Protobuf事件处理器
        /// </summary>
        /// <param name="eventId">事件ID</param>
        public void UnRegister(NetEvent eventId)
        {
            _handlers.Remove(eventId);
            Debug.Log($"[MessageProcessor] 注销事件 {eventId}");
        }
    }
}
