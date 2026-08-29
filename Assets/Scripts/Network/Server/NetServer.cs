using System;
using Events;
using Framework;
using Google.Protobuf;
using NetConnect;
using NetSync;
using UnityEngine;
using Utils;
using Ping = NetConnect.Ping;

namespace Network
{ 
    /// <summary>
    /// 服务端传输入口
    /// </summary>
    public class NetServer : SubSystemBase
    {
        public override int Priority => (int)SubSystemPriority.NetWorkManager;
        
        /// <summary>
        /// 服务端是否正在运行
        /// </summary>
        public bool IsRunning => _reliableTransport?.IsRunning == true || _fastTransport?.IsRunning == true;

        /// <summary>
        /// 检测当前是否存在快速传输通道
        /// </summary>
        public bool HasFastChannel => _fastTransport?.IsRunning == true;
        
        private IServerTransport _reliableTransport;
        private IServerTransport _fastTransport;
        private MessageProcessor _messageProcessor;
        private ClientManager _clientManager;
        private NetServerConfig _serverConfig;
        private float _serverPingAccumulator;
        private IServerNetHandler<Ping> _pingHandler;
        private IServerNetHandler<Pong> _pongHandler;
        private IServerNetHandler<Chat_Test> _debugChatHandler;
        private IServerNetHandler<Heart_Beat_Request> _heartBeatHandler;

        #region 事件
        public event Action<uint> OnClientRemoved;
        #endregion

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
                
                _reliableTransport.StartServer(_serverConfig.reliablePort);
                _fastTransport.StartServer(_serverConfig.fastPort);
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
        /// 登记服务端消息处理器（按实例，可多播）。
        /// </summary>
        public void RegisterHandler<T>(NetEvent eventId, IServerNetHandler<T> handler)
            where T : class, IMessage, new()
        {
            _messageProcessor.RegisterServer(eventId, handler);
        }

        /// <summary>
        /// 按处理器实例注销。
        /// </summary>
        public void UnregisterHandler(object handler)
        {
            _messageProcessor.Unregister(handler);
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
            var msg = NetUtils.Bytes2Proto(data);
            
            if (_clientManager.TryGetClientId(_reliableTransport.Type, transportSessionId, out var clientId))
            {
                _messageProcessor.HandleServerMessage(clientId, msg.Item1, msg.Item2);
            }
            else if (msg is { Item1: NetEvent.RELIABLE_CONNECT_REQUEST, Item2: Client_Reliable_Connect_Request request })
            {
                // RELIABLE_CONNECT_REQUEST需要特殊处理：此时transportSessionId尚未映射到clientId
                // 不能走标准的MessageProcessor分发，直接使用transportSessionId
                HandleReliableConnectRequest(transportSessionId, request);
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
                if (_clientManager.BindFastSession(request.Token, transportSessionId, out uint oldFastSessionId))
                {
                    // 关闭旧的KCP session（若有），避免悬挂连接
                    if (oldFastSessionId != 0) _fastTransport.Disconnect(oldFastSessionId);
                }
            }
        }
        
        private void HandleTransportError(string error)
        {
            Debug.LogError("[NetServer] Transport error: " + error);
        }
        
        private void HandleReliableClientConnect(uint transportSessionId)
        {
            Debug.Log($"[NetServer] Reliable transport session connected: {transportSessionId}");
        }

        private void HandleFastClientConnect(uint transportSessionId)
        {
            Debug.Log($"[NetServer] Fast transport session connected: {transportSessionId}");
        }

        private void HandleReliableClientDisconnect(uint transportSessionId)
        {
            if (_clientManager.UnBindReliableSession(transportSessionId, out uint clientId, Time.time))
            {
                Debug.Log($"[NetServer] Reliable session {transportSessionId} unbound from client {clientId}");
                return;
            }

            Debug.Log($"[NetServer] Reliable transport session disconnected: {transportSessionId}");
        }

        private void HandleFastClientDisconnect(uint transportSessionId)
        {
            if (_clientManager.UnbindFastSession(transportSessionId, out uint clientId, Time.time))
            {
                Debug.Log($"[NetServer] Fast session {transportSessionId} unbound from client {clientId}");
                return;
            }

            Debug.Log($"[NetServer] Fast transport session disconnected: {transportSessionId}");
        }
        
        #endregion

        #region 事件回调
        
        private void HandleClientRemoved(uint clientId)
        {
            OnClientRemoved?.Invoke(clientId);
        }
        
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

        /// <summary>
        /// 处理可靠连接请求
        /// token==0：新建客户端
        /// token有效 + clientId匹配 + client处于断线宽限期：恢复旧客户端
        /// token有效 + clientId匹配 + client仍在线：顶号（踢掉旧连接，绑定新TCP）
        /// </summary>
        private void HandleReliableConnectRequest(uint transportSessionId, Client_Reliable_Connect_Request request)
        {
            if (request.Token != 0 && request.ClientId != 0 && _clientManager.TryGetClient(request.Token, out var client) && client.Id == request.ClientId)
            {
                // --- token + clientId 双重匹配 ---
                if (client.IsConnected)
                {
                    // 顶号：client仍在线，踢掉旧session并重新绑定
                    uint oldReliableId = client.ReliableSessionId;
                    uint oldFastId = client.FastSessionId;

                    _clientManager.RebindReliableSession(request.Token, transportSessionId, out _);

                    // 关闭旧transport session，避免悬挂连接
                    if (oldReliableId != 0) _reliableTransport.Disconnect(oldReliableId);
                    if (oldFastId != 0) _fastTransport.Disconnect(oldFastId);

                    SendReliableConnectResponse(transportSessionId, client.Id, client.Token);
                    Debug.Log($"[NetServer] Client {client.Id} kicked and rebound, TCP session {transportSessionId}");
                }
                else if (client.DisconnectTime >= 0f)
                {
                    // 断线恢复：client在宽限期内，重新绑定TCP
                    _clientManager.RebindReliableSession(request.Token, transportSessionId, out uint oldReliableId);

                    // 关闭旧TCP session（若有），避免残留
                    if (oldReliableId != 0) _reliableTransport.Disconnect(oldReliableId);

                    SendReliableConnectResponse(transportSessionId, client.Id, client.Token);
                    Debug.Log($"[NetServer] Client {client.Id} recovered, TCP session rebound to {transportSessionId}");
                }
                else
                {
                    // 状态异常，降级为新建
                    Debug.LogWarning($"[NetServer] Recovery rejected: client {client.Id} in unexpected state, creating new");
                    CreateNewClient(transportSessionId);
                }
            }
            else
            {
                // Token为0 / token无效 / clientId不匹配 → 创建新客户端
                CreateNewClient(transportSessionId);
            }
        }

        private void SendReliableConnectResponse(uint transportSessionId, uint clientId, ulong token)
        {
            Client_Reliable_Connect_Response response = new Client_Reliable_Connect_Response()
            {
                ClientId = clientId,
                FastPort = _fastTransport.Port,
                Token = token,
            };
            _reliableTransport.Send(transportSessionId, NetUtils.Proto2Bytes(NetEvent.RELIABLE_CONNECT_RESPONSE, response));
        }

        private void CreateNewClient(uint transportSessionId)
        {
            var client = _clientManager.AddClient(transportSessionId);
            SendReliableConnectResponse(transportSessionId, client.Id, client.Token);
            Debug.Log($"[NetServer] New client {client.Id} created, TCP session {transportSessionId}");
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
            // 添加帧驱动延时
            long timeOffset = ping.Timestamp + (long)(Time.deltaTime * 1000f);
            Pong pong = new Pong { Timestamp = timeOffset };
            Send(clientId, NetEvent.PONG, pong);
        }

        /// <summary>
        /// 客户端Pong响应：服务端独立计算RTT
        /// </summary>
        private void HandleClientPong(uint clientId, Pong pong)
        {
            long nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            long nowMsWithFrameDelay = nowMs + (long)(Time.deltaTime * 1000f);
            float rtt = Math.Clamp((nowMsWithFrameDelay - pong.Timestamp) / 1000f, 0f, float.MaxValue);
            _clientManager.UpdateRTT(clientId, rtt);
        }
        
        #endregion

        #region 服务端Ping

        /// <summary>
        /// 服务端主动Ping：独立测量每个客户端的RTT
        /// </summary>
        private void UpdateServerPing(float deltaTime)
        {
            if (_serverConfig.rttStep <= 0f) return;
            
            _serverPingAccumulator += deltaTime;
            if (_serverPingAccumulator >= _serverConfig.rttStep)
            {
                _serverPingAccumulator -= _serverConfig.rttStep;
                SendServerPing();
            }
        }

        private void SendServerPing()
        {
            long nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            long nowMsWithFrameDelay = nowMs + (long)(Time.deltaTime * 1000f);
            Ping ping = new Ping { Timestamp = nowMsWithFrameDelay };
            byte[] data = NetUtils.Proto2Bytes(NetEvent.PING, ping);

            foreach (ClientManager.Client client in _clientManager.GetAllClients())
            {
                if (client.FastSessionId != 0)
                {
                    _fastTransport.Send(client.FastSessionId, data);
                }
            }
        }

        #endregion

        #region 生命周期

        public override void Init()
        {
            _serverConfig = NetServerConfig.Instance;
            _messageProcessor = new MessageProcessor();
            _clientManager = new ClientManager();
            _clientManager.OnClientRemoved += HandleClientRemoved;
            
            _reliableTransport = new TcpServerTransport(_serverConfig);
            _reliableTransport.OnDataReceived += HandleReliableDataReceived;
            _reliableTransport.OnTransportError += HandleTransportError;
            _reliableTransport.OnClientConnected += HandleReliableClientConnect;
            _reliableTransport.OnClientDisconnected += HandleReliableClientDisconnect;

            _fastTransport = new KcpServerTransport(_serverConfig);
            _fastTransport.OnDataReceived += HandleFastDataReceived;
            _fastTransport.OnTransportError += HandleTransportError;
            _fastTransport.OnClientConnected += HandleFastClientConnect;
            _fastTransport.OnClientDisconnected += HandleFastClientDisconnect;
        }

        public override void BindEvents()
        {
            _pingHandler = new DelegateServerNetHandler<Ping>(HandlePing);
            _pongHandler = new DelegateServerNetHandler<Pong>(HandleClientPong);
            _debugChatHandler = new DelegateServerNetHandler<Chat_Test>(HandleDebugChat);
            _heartBeatHandler = new DelegateServerNetHandler<Heart_Beat_Request>(HandleHeartBeatRequest);

            RegisterHandler(NetEvent.PING, _pingHandler);
            RegisterHandler(NetEvent.PONG, _pongHandler);
            RegisterHandler(NetEvent.CHAT_TEST, _debugChatHandler);
            RegisterHandler(NetEvent.HEART_BEAT_REQUEST, _heartBeatHandler);
            NetUtils.RegisterParser(NetEvent.FAST_CONNECT_REQUEST, Client_Fast_Connect_Request.Parser);
            NetUtils.RegisterParser(NetEvent.RELIABLE_CONNECT_REQUEST, Client_Reliable_Connect_Request.Parser);
            NetUtils.RegisterParser(NetEvent.PLAYER_INPUT, Player_Input.Parser);
            NetUtils.RegisterParser(NetEvent.WORLD_SNAPSHOT, World_Snapshot.Parser);
            NetUtils.RegisterParser(NetEvent.GAME_JOIN_REQUEST, Game_Join_Request.Parser);
            NetUtils.RegisterParser(NetEvent.GAME_JOIN_RESPONSE, Game_Join_Response.Parser);
        }

        public override void Update(float deltaTime)
        {
            _reliableTransport?.Update(deltaTime);
            _fastTransport?.Update(deltaTime);
            _clientManager?.CleanupDisconnectedClients(Time.time, _serverConfig.maxReconnectTime);
            UpdateServerPing(deltaTime);
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
            
            if (_clientManager != null)
            {
                _clientManager.OnClientRemoved -= HandleClientRemoved;
                _clientManager.Clear();
            }

            UnregisterHandler(_pingHandler);
            UnregisterHandler(_pongHandler);
            UnregisterHandler(_debugChatHandler);
            UnregisterHandler(_heartBeatHandler);
            _pingHandler = null;
            _pongHandler = null;
            _debugChatHandler = null;
            _heartBeatHandler = null;
        }

        #endregion

        private sealed class DelegateServerNetHandler<T> : IServerNetHandler<T> where T : class, IMessage, new()
        {
            private readonly Action<uint, T> _callback;

            public DelegateServerNetHandler(Action<uint, T> callback)
            {
                _callback = callback;
            }

            public void Handle(uint senderId, T message)
            {
                _callback(senderId, message);
            }
        }
    }
}
