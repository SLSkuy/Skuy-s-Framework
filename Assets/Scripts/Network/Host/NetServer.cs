using System;
using System.Text;
using Framework;
using Google.Protobuf;
using UnityEngine;
using Utils;

namespace Network
{ 
    /// <summary>
    /// 服务端传输入口
    /// </summary>
    public class NetServer : SubSystemBase
    {
        public override SubSystemPriority Priority => SubSystemPriority.NetWorkManager;
        
        public bool IsConnected => _serverTransport is {IsRunning: true};
        public TransportType TransportType => _serverTransport.Type;
        
        private IServerTransport _serverTransport;
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
                if (_serverTransport == null)
                {
                    Debug.LogError("[NetServer] Transport not initialized");
                    return;
                }
                
                _serverTransport.StartServer(_config.udpPort);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        public void StopServer()
        {
            _clientManager.Clear();
            _serverTransport.Stop();
        }

        /// <summary>
        /// 给单个客户端发送消息
        /// </summary>
        public void Send(uint clientId, IMessage message)
        {
            _serverTransport.Send(clientId, NetUtils.Proto2Bytes(message));
        }

        public void Send(uint clientId, byte[] data)
        {
            _serverTransport.Send(clientId, data);
        }

        /// <summary>
        /// 给一组客户端发送消息
        /// </summary>
        public void SendGroup(uint[] clientIds, IMessage message)
        {
            foreach (var clientId in clientIds)
            {
                _serverTransport.Send(clientId, NetUtils.Proto2Bytes(message));
            }
        }

        /// <summary>
        /// 为所有客户端广播消息
        /// </summary>
        /// <param name="message"></param>
        public void Broadcast(IMessage message)
        {
            _serverTransport.Broadcast(NetUtils.Proto2Bytes(message));
        }

        public void Broadcast(byte[] data)
        {
            _serverTransport.Broadcast(data);
        }
        
        /// <summary>
        /// 处理网络事件
        /// </summary>
        public void Register<T>(NetEvent eventId, Action<T> handler) where T : IMessage, new()
        {
            _messageProcessor.Register(eventId, handler);
        }

        /// <summary>
        /// 注销网络事件
        /// </summary>
        public void UnRegister(NetEvent eventId)
        {
            _messageProcessor.UnRegister(eventId);
        }

        /// <summary>
        /// 处理接收到的消息
        /// </summary>
        private void HandleDataReceived(uint clientId, byte[] data)
        {
            Debug.Log($"[NetServer] Received data from {clientId}, content: {Encoding.UTF8.GetString(data)}");
            
            _messageProcessor.HandleServerMessage(clientId, NetUtils.Bytes2Proto(data));
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

            // TODO: 接入TCP传输层用于处理需要可靠连接的消息
            
            TransportSettings settings = TransportSettings.FromConfig(_config);
            _serverTransport = new KcpServerTransport(settings);
            _serverTransport.OnDataReceived += HandleDataReceived;
            _serverTransport.OnTransportError += HandleTransportError;
            _serverTransport.OnClientConnected += HandleClientConnect;
            _serverTransport.OnClientDisconnected += HandleClientDisconnect;
        }

        public override void Update(float deltaTime)
        {
            _serverTransport.Update(deltaTime);
        }

        public override void Destroy()
        {
            if (_serverTransport != null)
            {
                _serverTransport.OnDataReceived -= HandleDataReceived;
                _serverTransport.OnTransportError -= HandleTransportError;
                _serverTransport.OnClientConnected -= HandleClientConnect;
                _serverTransport.OnClientDisconnected -= HandleClientDisconnect;
                _serverTransport.Dispose();
                _serverTransport = null;
            }
            
            _clientManager.Clear();
        }

        #endregion
    }
}
