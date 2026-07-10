using System;

namespace Network
{
    /// <summary>
    /// 服务端传输封装接口定义
    /// </summary>
    public interface IServerTransport : IDisposable
    {
        TransportType Type { get; }
        bool IsRunning { get; }
        event Action<uint> OnClientConnected;
        event Action<uint> OnClientDisconnected;
        event Action<uint, byte[]> OnDataReceived;
        event Action<string> OnTransportError;
        void StartServer(int port);
        void Send(uint clientId, byte[] data);
        void Broadcast(byte[] data);
        void Update(float deltaTime);
        void Stop();
    }
}
