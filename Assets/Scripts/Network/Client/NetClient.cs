using System;
using Framework;
using Google.Protobuf;
using NetConnect;
using UnityEngine;
using Ping = NetConnect.Ping;

namespace Network
{
    /// <summary>
    /// 客户端网络传输入口
    /// </summary>
    public class NetClient : SubSystemBase
    {
        public override SubSystemPriority Priority => SubSystemPriority.NetWorkManager;

        /// <summary>
        /// 当前网络延迟
        /// </summary>
        public float RTT => _lastRtt;

        private IClientTransport _reliableTransport;
        private IClientTransport _fastTransport;
        private MessageProcessor _messageProcessor;
        private NetConfig _config;

        // ========== 连接标识 ==========
        private uint _clientId;
        private ulong _token;
        private bool _tryReconnect;
        private float _reconnectAccumulator;
        private int _reconnectTimes;
        // ========== 连接标识 ==========

        // ========== 网络心跳 ==========
        private float _heartbeatAccumulator;
        private int _heartbeatMissCount;
        // ========== 网络心跳 ==========

        // ========== RTT ==========
        private float _pingAccumulator;
        private float _lastRtt;
        // ========== RTT ==========

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

            // 重置标识
            _clientId = 0;
            _token = 0;
            
            _heartbeatAccumulator = 0f;
            _heartbeatMissCount = 0;
            _pingAccumulator = 0f;
            _lastRtt = 0f;
            
            _tryReconnect = false;
            _reconnectAccumulator = 0f;
            _reconnectTimes = 0;
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

        private void HandleDebugChat(Chat_Test msg)
        {
            Debug.Log($"[NetClient] Chat Test From Server: {msg}");
        }
        
        /// <summary>
        /// 发送连接请求
        /// </summary>
        private void HandleConnected()
        {
            // 重置重连状态
            _tryReconnect = false;
            
            Client_Reliable_Connect_Request request = new Client_Reliable_Connect_Request()
            {
                ClientId = _clientId,
                Token = _token,
            };
            SendReliable(NetEvent.RELIABLE_CONNECT_REQUEST, request);
        }

        private void HandleReliableConnectResponse(Client_Reliable_Connect_Response response)
        {
            _clientId = response.ClientId;
            _token = response.Token;
            
            // 重置心跳和Ping状态
            _heartbeatAccumulator = 0f;
            _heartbeatMissCount = 0;
            _pingAccumulator = 0f;
            _lastRtt = 0f;
            
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

        /// <summary>
        /// TCP心跳响应：服务器确认存活，重置丢失计数
        /// </summary>
        private void HandleHeartBeatResponse(Heart_Beat_Response response)
        {
            _heartbeatMissCount = 0;
        }

        /// <summary>
        /// KCP Pong响应：计算RTT
        /// </summary>
        private void HandlePong(Pong pong)
        {
            // 添加帧驱动延时
            long sendMs = pong.Timestamp + (long)(Time.deltaTime * 1000f);
            long nowMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            
            // 帧间隔存在误差，防止延迟小于零 
            _lastRtt = Math.Clamp((nowMs - sendMs) / 1000f, 0f, float.MaxValue);
        }

        #endregion

        #region 心跳 / Ping

        /// <summary>
        /// 网络心跳：通过可靠通道发送HeartBeat，检测服务器存活
        /// </summary>
        private void SendHeartbeat()
        {
            _heartbeatMissCount++;

            if (_heartbeatMissCount >= _config.maxHeartbeatMisses)
            {
                Debug.LogWarning("[NetClient] Server heartbeat timeout, disconnecting...");
                
                // 尝试重连
                if (_config.autoReconnect) TryReconnect();
                else StopClient();
                
                return;
            }
            
            Heart_Beat_Request request = new Heart_Beat_Request();
            SendReliable(NetEvent.HEART_BEAT_REQUEST, request);
        }

        /// <summary>
        /// 延迟计算：通过快速通道测量RTT延迟
        /// </summary>
        private void SendPing()
        {
            long nowTicks = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            nowTicks += (long)(Time.deltaTime * 1000f); // 添加帧驱动延时
            Ping ping = new Ping { Timestamp = nowTicks };
            Send(NetEvent.PING, ping);
        }
        
        /// <summary>
        /// 网络心跳：检测连接存活
        /// </summary>
        /// <param name="deltaTime"></param>
        private void HeartBeat(float deltaTime)
        {
            if (_config.heartBeatStep > 0f)
            {
                _heartbeatAccumulator += deltaTime;
                if (_heartbeatAccumulator >= _config.heartBeatStep)
                {
                    _heartbeatAccumulator -= _config.heartBeatStep;
                    SendHeartbeat();
                }
            }
        }
        
        /// <summary>
        /// 网络延迟计算：测量RTT
        /// </summary>
        /// <param name="deltaTime"></param>
        private void ComputeRTT(float deltaTime)
        {
            if (_config.rttStep > 0f)
            {
                _pingAccumulator += deltaTime;
                if (_pingAccumulator >= _config.rttStep)
                {
                    _pingAccumulator -= _config.rttStep;
                    SendPing();
                }
            }
        }

        #endregion

        #region 重连逻辑

        private void TryReconnect()
        {
            _tryReconnect = true;
            _reconnectTimes = 0;
            _reconnectAccumulator = _config.reconnectInterval;
            
            _reliableTransport?.Stop();
            _fastTransport?.Stop();
        }

        private void DoReconnect(float deltaTime)
        {
            if (_config.reconnectInterval > 0f)
            {
                _reconnectAccumulator += deltaTime;
                if (_reconnectAccumulator >= _config.reconnectInterval)
                {
                    _reconnectAccumulator -= _config.reconnectInterval;
                    StartReliableConnect();
                    
                    _reconnectTimes++;
                    Debug.Log($"[NetClient] Reconnecting {_reconnectTimes} / {_config.maxReconnectCount}");
                    if (_reconnectTimes >= _config.maxReconnectCount)
                    {
                        // 连接次数超时
                        Debug.Log("[NetClient] Reconnect failed, client disconnected");
                        StopClient();
                    }
                }
            }
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
            RegNetHandler<Pong>(NetEvent.PONG, HandlePong);
            RegNetHandler<Chat_Test>(NetEvent.CHAT_TEST, HandleDebugChat);
            RegNetHandler<Client_Reliable_Connect_Response>(NetEvent.RELIABLE_CONNECT_RESPONSE, HandleReliableConnectResponse);
            RegNetHandler<Heart_Beat_Response>(NetEvent.HEART_BEAT_RESPONSE, HandleHeartBeatResponse);
        }

        public override void Update(float deltaTime)
        {
            _reliableTransport?.Update(deltaTime);
            _fastTransport?.Update(deltaTime);

            // 仅在可靠连接建立后（已获得clientId）才启用心跳/Ping
            if (_clientId == 0)
            {
                return;
            }

            if (_tryReconnect)
            {
                DoReconnect(deltaTime);
            }
            else
            {
                // 网络心跳
                HeartBeat(deltaTime);
                
                // 只有可靠连接时可用时才计算RTT，确保有可靠连接才启用快速连接
                // 防止服务端有无主的KCP会话连接
                if(_reliableTransport is { IsRunning: true }) ComputeRTT(deltaTime);
            }
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
