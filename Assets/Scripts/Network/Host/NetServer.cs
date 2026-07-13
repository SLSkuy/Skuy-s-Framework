using System;
using Framework;
using Google.Protobuf;
using NetConnect;
using UnityEngine;
using Ping = NetConnect.Ping;

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

        #region 消息发送

         /// <summary>
        /// 给单个客户端发送消息
        /// </summary>
        public void Send(uint clientId, NetEvent evt, IMessage message)
        {
            Send(clientId, NetUtils.Proto2Bytes(evt, message));
        }
        
        /// <summary>
        /// 使用可靠传输发送
        /// </summary>
        public void SendReliable(uint clientId, NetEvent evt, IMessage message)
        {
            SendReliable(clientId, NetUtils.Proto2Bytes(evt, message));
        }

        /// <summary>
        /// 给单个客户端发送消息
        /// </summary>
        public void Send(uint clientId, byte[] data)
        {
            if (!_clientManager.TryGetFastSessionId(clientId, out uint fastSessionId))
            {
                Debug.LogWarning($"[NetServer] Fast session not found for client {clientId}");
                return;
            }

            _fastTransport.Send(fastSessionId, data);
        }

        /// <summary>
        /// 使用可靠传输发送
        /// </summary>
        public void SendReliable(uint clientId, byte[] data)
        {
            if (!_clientManager.TryGetReliableSessionId(clientId, out uint reliableSessionId))
            {
                Debug.LogWarning($"[NetServer] Reliable session not found for client {clientId}");
                return;
            }

            _reliableTransport.Send(reliableSessionId, data);
        }

        /// <summary>
        /// 给一组客户端发送消息
        /// </summary>
        public void SendGroup(uint[] clientIds, NetEvent evt, IMessage message)
        {
            foreach (var clientId in clientIds)
            {
                Send(clientId, evt, message);
            }
        }

        /// <summary>
        /// 给一组客户端发送消息，使用可靠传输发送
        /// </summary>
        public void SendGroupReliable(uint[] clientIds, NetEvent evt, IMessage message)
        {
            foreach (var clientId in clientIds)
            {
                SendReliable(clientId, evt, message);
            }
        }

        /// <summary>
        /// 为所有客户端广播消息
        /// </summary>
        public void Broadcast(NetEvent evt, IMessage message)
        {
            Broadcast(NetUtils.Proto2Bytes(evt, message));
        }

        /// <summary>
        /// 为所有客户端广播消息,，使用可靠传输发送
        /// </summary>
        public void BroadcastReliable(NetEvent evt, IMessage message)
        {
            BroadcastReliable(NetUtils.Proto2Bytes(evt, message));
        }

        public void Broadcast(byte[] data)
        {
            foreach (ClientManager.Client client in _clientManager.GetAllClients())
            {
                if (client.FastSessionId != 0)
                {
                    _fastTransport.Send(client.FastSessionId, data);
                }
            }
        }

        /// <summary>
        /// 使用可靠传输(TCP)广播
        /// </summary>
        public void BroadcastReliable(byte[] data)
        {
            foreach (ClientManager.Client client in _clientManager.GetAllClients())
            {
                if (client.ReliableSessionId != 0)
                {
                    _reliableTransport.Send(client.ReliableSessionId, data);
                }
            }
        }

        #endregion
        
        #region 数据回调

        /// <summary>
        /// 处理接收到的可靠消息
        /// </summary>
        private void HandleReliableDataReceived(uint transportSessionId, byte[] data)
        {
            if (_clientManager.TryGetClientId(_reliableTransport.Type, transportSessionId, out var clientId))
            {
                var msg = NetUtils.Bytes2Proto(data);
                _messageProcessor.HandleServerMessage(clientId, msg.Item1, msg.Item2);
            }
        }

        /// <summary>
        /// 处理接收到的即时消息
        /// </summary>
        private void HandleFastDataReceived(uint transportSessionId, byte[] data)
        {
            var msg = NetUtils.Bytes2Proto(data);
            if (_clientManager.TryGetClientId(_fastTransport.Type, transportSessionId, out var clientId))
            {
                _messageProcessor.HandleServerMessage(clientId, msg.Item1, msg.Item2);
            }
            else if(msg is { Item1: NetEvent.FAST_CONNECT_REQUEST, Item2: Client_Fast_Connect_Request request })
            {
                // 特殊处理KCP连接请求
                _clientManager.BindFastSession(request.Token, transportSessionId);
            }
        }
        
        private void HandleTransportError(string error)
        {
            Debug.LogError("[NetServer] Transport error: " + error);
        }
        
        private void HandleReliableClientConnect(uint transportSessionId)
        {
            Debug.Log($"[NetServer] Reliable transport session connected: {transportSessionId}");
            
            // 添加新的客户端连接
            _clientManager.AddClient(transportSessionId);
        }

        private void HandleFastClientConnect(uint transportSessionId)
        {
            Debug.Log($"[NetServer] Fast transport session connected: {transportSessionId}");
        }

        private void HandleReliableClientDisconnect(uint transportSessionId)
        {
            if (_clientManager.UnBindReliableSession(transportSessionId, out uint clientId))
            {
                Debug.Log($"[NetServer] Reliable session {transportSessionId} unbound from client {clientId}");
                return;
            }

            Debug.Log($"[NetServer] Reliable transport session disconnected: {transportSessionId}");
        }

        private void HandleFastClientDisconnect(uint transportSessionId)
        {
            if (_clientManager.UnbindFastSession(transportSessionId, out uint clientId))
            {
                Debug.Log($"[NetServer] Fast session {transportSessionId} unbound from client {clientId}");
                return;
            }

            Debug.Log($"[NetServer] Fast transport session disconnected: {transportSessionId}");
        }
        
        #endregion

        #region 事件回调
        
        private void HandleDebugChat(uint clientId, Chat_Test msg)
        {
            if (_clientManager.TryGetClient(clientId, out var client))
            {
                Debug.Log($"[NetServer] Chat Test From Client: {clientId}\n" +
                          $"FastSessionId: {client.FastSessionId}\n" +
                          $"ReliableSessionId: {client.ReliableSessionId}\n" +
                          $"Token: {client.Token}\n" +
                          $"Message: {msg.Content}\n");
            }
        }

        private void HandleReliableConnectRequest(uint clientId, Client_Reliable_Connect_Request request)
        {
            if (_clientManager.TryGetClient(clientId, out var client))
            {
                // 发送回连接响应包
                Client_Reliable_Connect_Response response = new Client_Reliable_Connect_Response()
                {
                    ClientId = clientId,
                    FastPort = _fastTransport.Port,
                    Token = client.Token,
                };
                SendReliable(clientId, NetEvent.RELIABLE_CONNECT_RESPONSE, response);
            }
        }

        /// <summary>
        /// TCP心跳：客户端发来HeartBeat，服务端回复确认
        /// </summary>
        private void HandleHeartBeatRequest(uint clientId, Heart_Beat_Request request)
        {
            Heart_Beat_Response response = new Heart_Beat_Response();
            SendReliable(clientId, NetEvent.HEART_BEAT_RESPONSE, response);
        }

        /// <summary>
        /// KCP Ping：客户端测延迟，服务端原样返回时间戳
        /// </summary>
        private void HandlePing(uint clientId, Ping ping)
        {
            Pong pong = new Pong { Timestamp = ping.Timestamp };
            Send(clientId, NetEvent.PONG, pong);
        }
        
        #endregion

        #region 生命周期

        public override void Init()
        {
            _config = NetConfig.Instance;
            _messageProcessor = new MessageProcessor();
            _clientManager = new ClientManager();
            TransportSettings settings = TransportSettings.FromConfig(_config);
            
            _reliableTransport = new TcpServerTransport(settings);
            _reliableTransport.OnDataReceived += HandleReliableDataReceived;
            _reliableTransport.OnTransportError += HandleTransportError;
            _reliableTransport.OnClientConnected += HandleReliableClientConnect;
            _reliableTransport.OnClientDisconnected += HandleReliableClientDisconnect;

            _fastTransport = new KcpServerTransport(settings);
            _fastTransport.OnDataReceived += HandleFastDataReceived;
            _fastTransport.OnTransportError += HandleTransportError;
            _fastTransport.OnClientConnected += HandleFastClientConnect;
            _fastTransport.OnClientDisconnected += HandleFastClientDisconnect;
        }

        public override void BindEvents()
        {
            RegNetHandler<Ping>(NetEvent.PING, HandlePing);
            RegNetHandler<Chat_Test>(NetEvent.CHAT_TEST, HandleDebugChat);
            NetUtils.RegisterParser(NetEvent.FAST_CONNECT_REQUEST, Client_Fast_Connect_Request.Parser);
            RegNetHandler<Client_Reliable_Connect_Request>(NetEvent.RELIABLE_CONNECT_REQUEST, HandleReliableConnectRequest);
            RegNetHandler<Heart_Beat_Request>(NetEvent.HEART_BEAT_REQUEST, HandleHeartBeatRequest);
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
                _fastTransport.OnDataReceived -= HandleFastDataReceived;
                _fastTransport.OnTransportError -= HandleTransportError;
                _fastTransport.OnClientConnected -= HandleFastClientConnect;
                _fastTransport.OnClientDisconnected -= HandleFastClientDisconnect;
                _fastTransport.Dispose();
                _fastTransport = null;
            }

            if (_reliableTransport != null)
            {
                _reliableTransport.OnDataReceived -= HandleReliableDataReceived;
                _reliableTransport.OnTransportError -= HandleTransportError;
                _reliableTransport.OnClientConnected -= HandleReliableClientConnect;
                _reliableTransport.OnClientDisconnected -= HandleReliableClientDisconnect;
                _reliableTransport.Dispose();
                _reliableTransport = null;
            }
            
            _clientManager.Clear();
        }

        #endregion
    }
}
