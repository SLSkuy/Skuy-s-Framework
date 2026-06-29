using System;
using System.Collections.Concurrent;
using Framework;
using Google.Protobuf;
using UnityEngine;

namespace Network
{
    public class NetWorkManager : SubSystemBase
    {
        #region 属性
        public NetworkData NetworkConfig => _networkConfig;
        public string Ip => _networkConfig?.ip;
        public short TcpPort => _networkConfig?.tcpPort ?? 0;
        public short UdpPort => _networkConfig?.udpPort ?? 0;
        public float HeartBeatStep => _networkConfig?.heartBeatStep ?? 0;
        #endregion
        
        private NetworkData _networkConfig;
        public override SubSystemPriority Priority => SubSystemPriority.NetWorkManager;
        
        // 组件引用
        private TcpManager _tcp;
        private UdpManager _udp;
        private MessageProcessor _processor;
        
        // Unity主线程处理调用队列
        private readonly ConcurrentQueue<byte[]> _tcpQueue = new();
        private readonly ConcurrentQueue<byte[]> _udpQueue = new();
        
        /// <summary>
        /// UDP发送Protobuf事件
        /// </summary>
        public void SendUdp()
        {
            // TODO：委托UDP封装发送UDP包
        }

        /// <summary>
        /// TCP发送Protobuf事件
        /// </summary>
        public void SendTcp()
        {
            // TODO：委托TCP封装发送TCP包
        }

        /// <summary>
        /// 注册相应类的网络事件处理函数
        /// </summary>
        /// <param name="type">网络事件类型</param>
        /// <param name="handler">处理函数</param>
        /// <typeparam name="T">Protobuf事件类型</typeparam>
        public void RegisterEventHandler<T>(NetEvent type, Action<T> handler) where T : IMessage, new()
        {
            _processor.Register<T>(type, handler);
        }

        /// <summary>
        /// 注销网络事件处理函数
        /// </summary>
        /// <param name="type">网络事件类型</param>
        public void UnRegisterEventHandler(NetEvent type)
        {
            _processor.UnRegister(type);
        }

        /// <summary>
        /// 加载网络配置信息
        /// </summary>
        private void LoadNetworkConfig()
        {
            _networkConfig = Global.GetAsset<NetworkData>("Network/NetworkConfig");
        }

        /// <summary>
        /// 尝试与服务器建立连接
        /// </summary>
        public void ConnectToServer()
        {
            if (_networkConfig)
            {
                _udp.Start(_networkConfig.ip, _networkConfig.udpPort);
                _tcp.Connect(_networkConfig.ip, _networkConfig.tcpPort);
            }
            else
            {
                Debug.LogError($"[{GetType().Name}] Can not find NetworkData.");
            }
        }

        #region 生命周期

        public override void Init()
        {
            _processor = new MessageProcessor();

            _tcp = new TcpManager();
            _tcp.OnMessageReceived += data => _tcpQueue.Enqueue(data);

            _udp = new UdpManager();
            _udp.OnDataReceived += data => _udpQueue.Enqueue(data);
            
            LoadNetworkConfig();
        }

        public override void Update(float deltaTime)
        {
            // 处理操作事件
            while (_tcpQueue.Count > 0)
            {
                if (_tcpQueue.TryDequeue(out var data))
                {
                    _processor.DeserializeTcp(data);
                }
            }
            
            // 处理战局同步事件
            while (_udpQueue.Count > 0)
            {
                if (_udpQueue.TryDequeue(out var data))
                {
                    _processor.DeserializeUdp(data);
                }
            }
        }

        public override void Destroy()
        {
            _tcp?.Disconnect();
            _udp?.Stop();
        }
        
        #endregion
    }
}
