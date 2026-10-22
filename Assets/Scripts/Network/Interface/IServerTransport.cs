using System;

namespace Network
{
    /// <summary>
    /// 服务端传输封装接口定义
    /// </summary>
    public interface IServerTransport : IDisposable
    {
        TransportType Type { get; }
        ushort Port { get; }
        bool IsRunning { get; }
        event Action<uint> OnClientConnected;
        event Action<uint> OnClientDisconnected;
        event Action<uint, byte[]> OnDataReceived;
        event Action<string> OnTransportError;
        void StartServer(ushort port);
        void Stop();
        void Disconnect(uint sessionId);
        void Send(uint sessionId, byte[] data);
        void Update(float deltaTime);

        /// <summary>
        /// 连接层已用 token 认领该传输会话。尚未认领的快速会话按更短的时限关闭。
        /// </summary>
        void AcceptSession(uint sessionId);
    }
}
