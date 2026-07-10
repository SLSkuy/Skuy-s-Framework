using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

namespace Network
{
    public sealed class KcpClientTransport : IClientTransport
    {
        private readonly TransportSettings _settings;
        private UdpClient _udpClient;
        private KcpPeer _peer;
        private IPEndPoint _serverEndPoint;
        private bool _isDisposed;

        public TransportType Type => TransportType.KCP;
        public bool IsRunning { get; private set; }

        public event Action<byte[]> OnDataReceived;
        public event Action<string> OnTransportError;

        public KcpClientTransport(TransportSettings settings)
        {
            _settings = settings ?? TransportSettings.Default;
        }

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
                _peer = new KcpPeer(_settings.Conv, _serverEndPoint, _settings, SendRaw);
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
            if (!IsRunning || _peer == null)
            {
                RaiseError("KCP client is not running.");
                return;
            }

            try
            {
                _peer.Send(data);
            }
            catch (Exception ex)
            {
                RaiseError($"KCP client send failed: {ex.Message}");
            }
        }

        public void Update(float deltaTime)
        {
            if (!IsRunning || _udpClient == null || _peer == null)
            {
                return;
            }

            try
            {
                ReceiveAvailableDatagrams();
                DateTimeOffset now = DateTimeOffset.UtcNow;
                _peer.Update(now);
                DispatchReceivedMessages();
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

            _peer?.Dispose();
            _peer = null;

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

        private void ReceiveAvailableDatagrams()
        {
            while (_udpClient.Available > 0)
            {
                IPEndPoint remoteEndPoint = null;
                byte[] datagram = _udpClient.Receive(ref remoteEndPoint);
                _peer.Input(datagram);
            }
        }

        private void DispatchReceivedMessages()
        {
            while (_peer.TryReceive(out byte[] data))
            {
                OnDataReceived?.Invoke(data);
            }
        }

        private void SendRaw(KcpPeer peer, byte[] data, int length)
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
    }
}
