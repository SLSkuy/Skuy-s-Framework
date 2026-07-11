using System;
using System.Net;
using System.Net.Sockets;

namespace Network
{
    /// <summary>
    /// KCP客户端传输封装
    /// </summary>
    public sealed class KcpClientTransport : IClientTransport
    {
        public TransportType Type => TransportType.KCP;
        public bool IsRunning { get; private set; }

        private readonly TransportSettings _settings;
        private UdpClient _udpClient;
        private KcpSession _session;
        private IPEndPoint _serverEndPoint;
        private bool _isDisposed;

        public event Action<byte[]> OnDataReceived;
        public event Action<string> OnTransportError;

        public KcpClientTransport(TransportSettings settings)
        {
            _settings = settings ?? TransportSettings.Default;
        }

        #region 暴露接口

        public void StartClient(string host, short port)
        {
            ThrowIfDisposed();
            Stop();

            try
            {
                IPAddress[] addresses = Dns.GetHostAddresses(host);
                if (addresses.Length == 0)
                {
                    RaiseError($"KCP client failed to resolve host: {host}");
                    return;
                }

                IPAddress address = SelectAddress(addresses);
                _serverEndPoint = new IPEndPoint(address, port);
                _udpClient = new UdpClient(address.AddressFamily);
                _udpClient.Connect(_serverEndPoint);
                _session = new KcpSession(_settings.conv, _serverEndPoint, _settings, Send);
                IsRunning = true;
            }
            catch (Exception ex)
            {
                RaiseError($"KCP client start failed: {ex.Message}");
                Stop();
            }
        }

        public void Send(byte[] data)
        {
            if (!IsRunning || _session == null)
            {
                RaiseError("KCP client is not running.");
                return;
            }

            try
            {
                int result = _session.Send(data);
                if (result < 0)
                {
                    RaiseError($"KCP client send failed with code: {result}");
                }
            }
            catch (Exception ex)
            {
                RaiseError($"KCP client send failed: {ex.Message}");
            }
        }

        public void Update(float deltaTime)
        {
            if (!IsRunning || _udpClient == null || _session == null)
            {
                return;
            }

            try
            {
                ReceiveDatagrams();
                DateTimeOffset now = DateTimeOffset.UtcNow;
                _session.Update(now);
                DispatchMessages();
            }
            catch (SocketException ex)
            {
                RaiseError($"KCP client socket error: {ex.Message}");
            }
            catch (ObjectDisposedException)
            {
                // 正常退出
            }
            catch (Exception ex)
            {
                RaiseError($"KCP client update failed: {ex.Message}");
            }
        }

        public void Stop()
        {
            IsRunning = false;

            _session?.Dispose();
            _session = null;

            _udpClient?.Close();
            _udpClient?.Dispose();
            _udpClient = null;
            _serverEndPoint = null;
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

        private void ReceiveDatagrams()
        {
            while (_udpClient.Available > 0)
            {
                IPEndPoint remoteEndPoint = null;
                byte[] datagram = _udpClient.Receive(ref remoteEndPoint);
                int result = _session.Input(datagram);
                if (result < 0)
                {
                    RaiseError($"KCP client input failed with code: {result}");
                }
            }
        }

        private void DispatchMessages()
        {
            while (_session.TryReceive(out byte[] data))
            {
                OnDataReceived?.Invoke(data);
            }
        }

        private void Send(KcpSession session, byte[] data, int length)
        {
            if (!IsRunning || _udpClient == null)
            {
                return;
            }

            _udpClient.Send(data, length);
        }

        private void RaiseError(string message)
        {
            OnTransportError?.Invoke(message);
        }

        private static IPAddress SelectAddress(IPAddress[] addresses)
        {
            foreach (IPAddress address in addresses)
            {
                if (address.AddressFamily == AddressFamily.InterNetwork)
                {
                    return address;
                }
            }

            return addresses[0];
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(KcpClientTransport));
            }
        }

        #endregion
    }
}
