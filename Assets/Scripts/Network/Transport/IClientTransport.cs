using System;

namespace Network
{
    /// <summary>
    /// 客户端传输封装接口定义
    /// </summary>
    public interface IClientTransport : IDisposable
    {
        TransportType Type { get; }
        bool IsRunning { get; }
        event Action<byte[]> OnDataReceived;
        event Action<string> OnTransportError;
        void StartClient(string host, int port, TransportSettings settings);
        void Send(byte[] data);
        void Update(float deltaTime);
        void Stop();
    }
}
