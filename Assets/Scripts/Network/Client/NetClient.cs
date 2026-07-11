using System;
using Framework;
using Google.Protobuf;
using UnityEngine;
using Utils;

namespace Network
{
    /// <summary>
    /// 客户端网络传输入口
    /// </summary>
    public class NetClient : SubSystemBase
    {
        public override SubSystemPriority Priority => SubSystemPriority.NetWorkManager;
        
        public bool IsConnected => _clientTransport is { IsRunning: true };
        public TransportType TransportType => _clientTransport.Type;
        
        private IClientTransport _clientTransport;
        private MessageProcessor _messageProcessor;
        private NetConfig _config;

        /// <summary>
        /// 开启客户端连接
        /// </summary>
        /// <param name="host">服务器IP</param>
        /// <param name="port">服务器端口</param>
        public void StartClient(string host, short port)
        {
            try
            {
                if (_clientTransport == null)
                {
                    Debug.LogError("[NetClient] Transport not initialized.");
                    return;
                }
                
                _clientTransport.StartClient(host, port);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        public void StopClient()
        {
            _clientTransport?.Stop();
        }
        
        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="message"></param>
        public void Send(IMessage message)
        {
            _clientTransport?.Send(NetUtils.Proto2Bytes(message));
        }
        
        /// <summary>
        /// 处理网络事件
        /// </summary>
        public void Register<T>(NetEvent eventId, Action<T> handler) where T : IMessage, new()
        {
            _messageProcessor?.Register(eventId, handler);
        }

        /// <summary>
        /// 注销网络事件
        /// </summary>
        public void UnRegister(NetEvent eventId)
        {
            _messageProcessor?.UnRegister(eventId);
        }

        /// <summary>
        /// 处理接收到的消息
        /// </summary>
        /// <param name="data"></param>
        private void HandleDataReceived(byte[] data)
        {
            _messageProcessor.HandleMessage(NetUtils.Bytes2Proto(data));
        }

        private void HandleTransportError(string error)
        {
            Debug.LogError("[NetClient] Transport error: " + error);
        }

        #region 生命周期

        public override void Init()
        {
            _config = NetConfig.Instance;
            _messageProcessor = new MessageProcessor();

            TransportSettings settings = TransportSettings.FromConfig(_config);
            _clientTransport = new KcpClientTransport(settings);
            _clientTransport.OnDataReceived += HandleDataReceived;
            _clientTransport.OnTransportError += HandleTransportError;
        }

        public override void Update(float deltaTime)
        {
            _clientTransport?.Update(deltaTime);
        }

        public override void Destroy()
        {
            if (_clientTransport != null)
            {
                _clientTransport.OnDataReceived -= HandleDataReceived;
                _clientTransport.OnTransportError -= HandleTransportError;
                _clientTransport.Dispose();
                _clientTransport = null;
            }
            
            _messageProcessor = null;
            _config = null;
        }

        #endregion
    }
}
