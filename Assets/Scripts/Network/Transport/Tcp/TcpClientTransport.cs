using System;
using System.Collections.Concurrent;

namespace Network
{
    /// <summary>
    /// TCP客户端传输封装
    /// </summary>
    public sealed class TcpClientTransport : IClientTransport
    {
        public TransportType Type => TransportType.TCP;
        public bool IsRunning { get; private set; }

        private readonly ConcurrentQueue<byte[]> _receivedMessages = new ConcurrentQueue<byte[]>();
        private readonly ConcurrentQueue<string> _errors = new ConcurrentQueue<string>();
        private TcpSession _session;
        private bool _isDisposed;
        private volatile bool _disconnected;    // 主线程派发，防止跨线程访问

        public event Action<byte[]> OnDataReceived;
        public event Action<string> OnTransportError;
        public event Action OnConnected;
        public event Action OnDisconnected;

        #region 暴露接口
        
        public void StartClient(string host, short port)
        {
            ThrowIfDisposed();
            Stop();

            _session = new TcpSession();
            _session.OnMessageReceived += HandleMessageReceived;
            _session.OnDisconnected += HandleDisconnected;
            _session.OnError += HandleError;

            try
            {
                _session.Connect(host, port);
                IsRunning = true;
                OnConnected?.Invoke();
            }
            catch (Exception ex)
            {
                Stop();
                OnTransportError?.Invoke(ex.Message);
            }
        }

        public void Send(byte[] data)
        {
            if (!IsRunning || _session == null)
            {
                RaiseError("TCP client is not running.");
                return;
            }

            _session.EnqueueSend(data);
        }

        public void Update(float deltaTime)
        {
            while (_receivedMessages.TryDequeue(out byte[] data))
            {
                OnDataReceived?.Invoke(data);
            }

            while (_errors.TryDequeue(out string error))
            {
                OnTransportError?.Invoke(error);
            }

            // 断连事件延迟到主线程派发，避免网络线程直接修改NetClient状态
            if (_disconnected)
            {
                _disconnected = false;
                OnDisconnected?.Invoke();
            }
        }

        public void Stop()
        {
            IsRunning = false;
            _disconnected = false;  // 清除挂起的断线标记，防止显式Stop后Update仍触发OnDisconnected

            if (_session != null)
            {
                _session.OnMessageReceived -= HandleMessageReceived;
                _session.OnDisconnected -= HandleDisconnected;
                _session.OnError -= HandleError;
                _session.Dispose();
                _session = null;
            }

            ClearQueues();
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            Stop();
            _isDisposed = true;
        }
        
        #endregion

        #region 内部管理方法

        private void HandleMessageReceived(byte[] data)
        {
            _receivedMessages.Enqueue(data);
        }

        private void HandleDisconnected()
        {
            IsRunning = false;
            _disconnected = true;
        }

        private void HandleError(string error)
        {
            _errors.Enqueue(error);
        }

        private void RaiseError(string message)
        {
            _errors.Enqueue(message);
        }

        private void ClearQueues()
        {
            while (_receivedMessages.TryDequeue(out _))
            {
            }

            while (_errors.TryDequeue(out _))
            {
            }
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(TcpClientTransport));
            }
        }

        #endregion
    }
}
