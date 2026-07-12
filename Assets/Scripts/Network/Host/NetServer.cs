using System;
using System.Text;
using Framework;
using Google.Protobuf;
using UnityEngine;

namespace Network
{ 
    /// <summary>
    /// 服务端传输入口
    /// </summary>
    public class NetServer : SubSystemBase
    {
        public override SubSystemPriority Priority => SubSystemPriority.NetWorkManager;
        
        private IServerTransport _reliableTransport;
        private IServerTransport _fastTransport;
        private ClientManager _clientManager;
        private MessageProcessor _messageProcessor;
        private NetConfig _config;

        /// <summary>
        /// 开启服务器
        /// </summary>
        public void StartServer()
        {
            try
            {
                if (_reliableTransport == null || _fastTransport == null)
                {
                    Debug.LogError("[NetServer] Transport not initialized");
                    return;
                }
                
                _reliableTransport.StartServer(_config.reliablePort);
                _fastTransport.StartServer(_config.fastPort);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        public void StopServer()
        {
            _clientManager?.Clear();
            _reliableTransport?.Stop();
            _fastTransport?.Stop();
        }

        /// <summary>
        /// 给单个客户端发送消息
        /// </summary>
        public void Send(uint clientId, NetEvent evt, IMessage message)
        {
            _fastTransport.Send(clientId, NetUtils.Proto2Bytes(evt, message));
        }
        
        /// <summary>
        /// 使用可靠传输发送
        /// </summary>
        public void SendReliable(uint clientId, NetEvent evt, IMessage message)
        {
            _reliableTransport.Send(clientId, NetUtils.Proto2Bytes(evt, message));
        }

        /// <summary>
        /// 给单个客户端发送消息
        /// </summary>
        public void Send(uint clientId, byte[] data)
        {
            _fastTransport.Send(clientId, data);
        }

        /// <summary>
        /// 使用可靠传输发送
        /// </summary>
        public void SendReliable(uint clientId, byte[] data)
        {
            _reliableTransport.Send(clientId, data);
        }

        /// <summary>
        /// 给一组客户端发送消息
        /// </summary>
        public void SendGroup(uint[] clientIds, NetEvent evt, IMessage message)
        {
            foreach (var clientId in clientIds)
            {
                _fastTransport.Send(clientId, NetUtils.Proto2Bytes(evt, message));
            }
        }

        /// <summary>
        /// 给一组客户端发送消息，使用可靠传输发送
        /// </summary>
        public void SendGroupReliable(uint[] clientIds, NetEvent evt, IMessage message)
        {
            foreach (var clientId in clientIds)
            {
                _reliableTransport.Send(clientId, NetUtils.Proto2Bytes(evt, message));
            }
        }

        /// <summary>
        /// 为所有客户端广播消息
        /// </summary>
        public void Broadcast(NetEvent evt, IMessage message)
        {
            _fastTransport.Broadcast(NetUtils.Proto2Bytes(evt, message));
        }

        /// <summary>
        /// 为所有客户端广播消息,，使用可靠传输发送
        /// </summary>
        public void BroadcastReliable(NetEvent evt, IMessage message)
        {
            _reliableTransport.Broadcast(NetUtils.Proto2Bytes(evt, message));
        }

        public void Broadcast(byte[] data)
        {
            _fastTransport.Broadcast(data);
        }

        /// <summary>
        /// 使用可靠传输(TCP)广播
        /// </summary>
        public void BroadcastReliable(byte[] data)
        {
            _reliableTransport.Broadcast(data);
        }
        
        /// <summary>
        /// 处理网络事件
        /// </summary>
        public void RegNetHandler<T>(NetEvent eventId, Action<uint, T> handler) where T : IMessage, new()
        {
            _messageProcessor.RegisterServer(eventId, handler);
        }

        /// <summary>
        /// 注销网络事件
        /// </summary>
        public void UnRegNetHandler(NetEvent eventId)
        {
            _messageProcessor.UnRegisterServer(eventId);
        }

        /// <summary>
        /// 处理接收到的消息
        /// </summary>
        private void HandleDataReceived(uint clientId, byte[] data)
        {
            Debug.Log($"[NetServer] Received data from {clientId}, content: {Encoding.UTF8.GetString(data)}");
            var msg = NetUtils.Bytes2Proto(data);
            _messageProcessor.HandleServerMessage(clientId, msg.Item1, msg.Item2);
        }

        private void HandleTransportError(string error)
        {
            Debug.LogError("[NetServer] Transport error: " + error);
        }

        private void HandleClientConnect(uint clientId)
        {
            
        }

        private void HandleClientDisconnect(uint clientId)
        {
            
        }

        #region 生命周期

        public override void Init()
        {
            _config = NetConfig.Instance;
            _messageProcessor = new MessageProcessor();
            _clientManager = new ClientManager();
            TransportSettings settings = TransportSettings.FromConfig(_config);
            
            _reliableTransport = new TcpServerTransport();
            _reliableTransport.OnDataReceived += HandleDataReceived;
            _reliableTransport.OnTransportError += HandleTransportError;
            _reliableTransport.OnClientConnected += HandleClientConnect;
            _reliableTransport.OnClientDisconnected += HandleClientDisconnect;

            _fastTransport = new KcpServerTransport(settings);
            _fastTransport.OnDataReceived += HandleDataReceived;
            _fastTransport.OnTransportError += HandleTransportError;
            _fastTransport.OnClientConnected += HandleClientConnect;
            _fastTransport.OnClientDisconnected += HandleClientDisconnect;
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
                _fastTransport.OnClientConnected -= HandleClientConnect;
                _fastTransport.OnClientDisconnected -= HandleClientDisconnect;
                _fastTransport.Dispose();
                _fastTransport = null;
            }

            if (_reliableTransport != null)
            {
                _reliableTransport.OnDataReceived -= HandleDataReceived;
                _reliableTransport.OnTransportError -= HandleTransportError;
                _reliableTransport.OnClientConnected -= HandleClientConnect;
                _reliableTransport.OnClientDisconnected -= HandleClientDisconnect;
                _reliableTransport.Dispose();
                _reliableTransport = null;
            }
            
            _clientManager.Clear();
        }

        #endregion
    }
}
