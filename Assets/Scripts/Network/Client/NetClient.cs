using System;
using System.Text;
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
        
        private IClientTransport _reliableTransport;
        private IClientTransport _fastTransport;
        private MessageProcessor _messageProcessor;
        private NetConfig _config;

        /// <summary>
        /// 开启可靠连接
        /// </summary>
        public void StartReliableConnect()
        {
            try
            {
                if (_reliableTransport == null)
                {
                    Debug.LogError("[NetClient] Transport not initialized.");
                    return;
                }
                
                _reliableTransport.StartClient(_config.ip, _config.reliablePort);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        /// <summary>
        /// 开启即使相应连接
        /// </summary>
        public void StartFastConnect(string ip, short port)
        {
            try
            {
                if (_fastTransport == null)
                {
                    Debug.LogError("[NetClient] Transport not initialized.");
                    return;
                }

                _fastTransport.StartClient(ip, port);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        /// <summary>
        /// 开启客户端连接
        /// </summary>
        public void StartClient()
        {
            StartReliableConnect();
            StartFastConnect(_config.ip, _config.fastPort);
        }

        public void StopClient()
        {
            _fastTransport?.Stop();
            _reliableTransport?.Stop();
        }

        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="message">消息体</param>
        /// <param name="reliable">是否为可靠消息</param>
        public void Send(IMessage message, bool reliable = true)
        {
            if (reliable)
            {
                _reliableTransport?.Send(NetUtils.Proto2Bytes(message));
            }
            else
            {
                _fastTransport?.Send(NetUtils.Proto2Bytes(message));
            }
        }

        public void Send(byte[] data)
        {
            _fastTransport?.Send(data);
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
            Debug.Log($"[NetClient] 收到服务端消息: {Encoding.UTF8.GetString(data)}");
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

            _reliableTransport = new TcpClientTransport();
            _reliableTransport.OnDataReceived += HandleDataReceived;
            _reliableTransport.OnTransportError += HandleTransportError;

            _fastTransport = new KcpClientTransport(settings);
            _fastTransport.OnDataReceived += HandleDataReceived;
            _fastTransport.OnTransportError += HandleTransportError;
        }

        public override void Update(float deltaTime)
        {
            _reliableTransport?.Update(deltaTime);
            _fastTransport?.Update(deltaTime);
        }

        public override void Destroy()
        {
            if (_fastTransport != null)
            {
                _fastTransport.OnDataReceived -= HandleDataReceived;
                _fastTransport.OnTransportError -= HandleTransportError;
                _fastTransport.Dispose();
                _fastTransport = null;
            }

            if (_reliableTransport != null)
            {
                _reliableTransport.OnDataReceived -= HandleDataReceived;
                _reliableTransport.OnTransportError -= HandleTransportError;
                _reliableTransport.Dispose();
                _reliableTransport = null;
            }
            
            _messageProcessor = null;
            _config = null;
        }

        #endregion
    }
}
