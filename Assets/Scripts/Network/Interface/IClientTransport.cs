using System;

namespace Network
{
    /// <summary>
    /// 客户端传输封装接口定义
    /// </summary>
    public interface IClientTransport : IDisposable
    {
        /// <summary>
        /// 传输协议
        /// </summary>
        TransportType Type { get; }
        
        /// <summary>
        /// 是否处于运行状态
        /// </summary>
        bool IsRunning { get; }
        
        /// <summary>
        /// 数据接收回调
        /// </summary>
        event Action<byte[]> OnDataReceived;
        
        /// <summary>
        /// 传输错误回调
        /// </summary>
        event Action<string> OnTransportError;
        
        /// <summary>
        /// 开始连接
        /// </summary>
        /// <param name="host">目标IP</param>
        /// <param name="port">目标端口</param>
        void StartClient(string host, short port);
        
        /// <summary>
        /// 发送数据
        /// </summary>
        /// <param name="data"></param>
        void Send(byte[] data);
        
        /// <summary>
        /// 更新接收缓冲
        /// </summary>
        /// <param name="deltaTime"></param>
        void Update(float deltaTime);
        
        /// <summary>
        /// 关闭连接
        /// </summary>
        void Stop();
    }
}
