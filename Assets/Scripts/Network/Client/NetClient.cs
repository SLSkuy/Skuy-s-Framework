using System;
using Framework;
using Google.Protobuf;
using NetConnect;
using UnityEngine;

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

        // ========== 连接标识 ==========
        private uint _clientId;
        private ulong _token;
        // ========== 连接标识 ==========

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

        public void StopClient()
        {
            _fastTransport?.Stop();
            _reliableTransport?.Stop();
        }
        
        /// <summary>
        /// 处理网络事件
        /// </summary>
        public void RegNetHandler<T>(NetEvent eventId, Action<T> handler) where T : IMessage, new()
        {
            _messageProcessor?.Register(eventId, handler);
        }

        /// <summary>
        /// 注销网络事件
        /// </summary>
        public void UnRegNetHandler(NetEvent eventId)
        {
            _messageProcessor?.UnRegister(eventId);
        }

        #region 消息发送

        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="evt">事件类型</param>
        /// <param name="message">消息体</param>
        public void Send(NetEvent evt, IMessage message)
        {
            _fastTransport?.Send(NetUtils.Proto2Bytes(evt, message));
        }

        /// <summary>
        /// 使用可靠传输发送
        /// </summary>
        public void SendReliable(NetEvent evt, IMessage message)
        {
            _reliableTransport?.Send(NetUtils.Proto2Bytes(evt, message));
        }

        public void Send(byte[] data)
        {
            _fastTransport?.Send(data);
        }

        /// <summary>
        /// 使用可靠传输(TCP)发送
        /// </summary>
        public void SendReliable(byte[] data)
        {
            _reliableTransport?.Send(data);
        }

        #endregion

        #region 传输回调

        /// <summary>
        /// 处理接收到的消息
        /// </summary>
        /// <param name="data"></param>
        private void HandleDataReceived(byte[] data)
        {
            var msg = NetUtils.Bytes2Proto(data);
            _messageProcessor.HandleMessage(msg.Item1, msg.Item2);
        }

        private void HandleTransportError(string error)
        {
            Debug.LogError("[NetClient] Transport error: " + error);
        }

        #endregion

        #region 事件回调

        /// <summary>
        /// 发送连接请求
        /// </summary>
        private void HandleConnected()
        {
            Client_Reliable_Connect_Request request = new Client_Reliable_Connect_Request();
            SendReliable(NetEvent.RELIABLE_CONNECT_REQUEST, request);
        }

        private void HandleReliableConnectResponse(Client_Reliable_Connect_Response response)
        {
            _clientId = response.ClientId;
            _token = response.Token;
            
            // 开启实时连接
            StartFastConnect(_config.ip, (short)response.FastPort);
            
            // 发送快速连接请求
            Client_Fast_Connect_Request request = new Client_Fast_Connect_Request()
            {
                ClientId = _clientId,
                Token = _token
            };
            Send(NetEvent.FAST_CONNECT_REQUEST, request);
        }
        
        private void HandleDebugChat(Chat_Test msg)
        {
            Debug.Log($"[NetClient] Chat Test From Server: {msg}");
        }

        #endregion

        #region 生命周期

        public override void Init()
        {
            _config = NetConfig.Instance;
            _messageProcessor = new MessageProcessor();
            TransportSettings settings = TransportSettings.FromConfig(_config);

            _reliableTransport = new TcpClientTransport();
            _reliableTransport.OnDataReceived += HandleDataReceived;
            _reliableTransport.OnTransportError += HandleTransportError;
            _reliableTransport.OnConnected += HandleConnected;

            _fastTransport = new KcpClientTransport(settings);
            _fastTransport.OnDataReceived += HandleDataReceived;
            _fastTransport.OnTransportError += HandleTransportError;
        }

        public override void BindEvents()
        {
            RegNetHandler<Client_Reliable_Connect_Response>(NetEvent.RELIABLE_CONNECT_RESPONSE, HandleReliableConnectResponse);
            RegNetHandler<Chat_Test>(NetEvent.CHAT_TEST, HandleDebugChat);
        }

        public override void Update(float deltaTime)
        {
            _reliableTransport?.Update(deltaTime);
            _fastTransport?.Update(deltaTime);
        }

        public override void Destroy()
        {
            if (_reliableTransport != null)
            {
                _reliableTransport.OnDataReceived -= HandleDataReceived;
                _reliableTransport.OnTransportError -= HandleTransportError;
                _reliableTransport.Dispose();
                _reliableTransport = null;
            }
            
            if (_fastTransport != null)
            {
                _fastTransport.OnDataReceived -= HandleDataReceived;
                _fastTransport.OnTransportError -= HandleTransportError;
                _fastTransport.Dispose();
                _fastTransport = null;
            }
            
            _messageProcessor = null;
            _config = null;
        }

        #endregion
    }
}
