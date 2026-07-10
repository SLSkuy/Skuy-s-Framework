using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace Network
{
    public sealed class KcpServerTransport : IServerTransport
    {
        private const int KcpHeaderSize = 24;

        private sealed class ClientSession
        {
            public uint ClientId;
            public KcpPeer Peer;
            public string EndPointKey;
        }

        private readonly TransportSettings _settings;
        private readonly Dictionary<uint, ClientSession> _clientsById = new Dictionary<uint, ClientSession>();
        private readonly Dictionary<string, ClientSession> _clientsByEndPoint = new Dictionary<string, ClientSession>();
        private UdpClient _udpServer;
        private uint _nextClientId = 1;
        private bool _isDisposed;

        public TransportType Type => TransportType.KCP;
        public bool IsRunning { get; private set; }

        public event Action<uint> OnClientConnected;
        public event Action<uint> OnClientDisconnected;
        public event Action<uint, byte[]> OnDataReceived;
        public event Action<string> OnTransportError;

        public KcpServerTransport(TransportSettings settings)
        {
            _settings = settings ?? TransportSettings.Default;
        }

        public void StartServer(int port)
        {
            ThrowIfDisposed();
            Stop();

            try
            {
                _udpServer = new UdpClient(port);
                IsRunning = true;
            }
            catch (Exception ex)
            {
                RaiseError($"KCP server start failed: {ex.Message}");
                Stop();
            }
        }

        public void Send(uint clientId, byte[] data)
        {
            if (!IsRunning)
            {
                RaiseError("KCP server is not running.");
                return;
            }

            if (_clientsById.TryGetValue(clientId, out ClientSession session))
            {
                session.Peer.Send(data);
            }
        }

        public void Broadcast(byte[] data)
        {
            foreach (ClientSession session in _clientsById.Values)
            {
                session.Peer.Send(data);
            }
        }

        public void Update(float deltaTime)
        {
            if (!IsRunning || _udpServer == null)
            {
                return;
            }

            try
            {
                ReceiveAvailableDatagrams();
                DateTimeOffset now = DateTimeOffset.UtcNow;
                UpdateClients(now);
                RemoveTimedOutClients(now);
            }
            catch (SocketException ex)
            {
                RaiseError($"KCP server socket error: {ex.Message}");
            }
            catch (ObjectDisposedException)
            {
                // 正常退出
            }
            catch (Exception ex)
            {
                RaiseError($"KCP server update failed: {ex.Message}");
            }
        }

        public void Stop()
        {
            IsRunning = false;

            foreach (ClientSession session in _clientsById.Values)
            {
                session.Peer.Dispose();
            }

            _clientsById.Clear();
            _clientsByEndPoint.Clear();

            _udpServer?.Close();
            _udpServer?.Dispose();
            _udpServer = null;
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
            while (_udpServer.Available > 0)
            {
                IPEndPoint remoteEndPoint = null;
                byte[] datagram = _udpServer.Receive(ref remoteEndPoint);
                ClientSession session = GetOrCreateSession(remoteEndPoint, datagram);
                if (session == null)
                {
                    continue;
                }

                session.Peer.RemoteEndPoint = remoteEndPoint;
                session.Peer.Input(datagram);
            }
        }

        private ClientSession GetOrCreateSession(IPEndPoint remoteEndPoint, byte[] datagram)
        {
            string endPointKey = remoteEndPoint.ToString();
            if (_clientsByEndPoint.TryGetValue(endPointKey, out ClientSession session))
            {
                return session;
            }

            if (datagram == null || datagram.Length < KcpHeaderSize)
            {
                return null;
            }

            uint conv = ReadConv(datagram);
            session = new ClientSession
            {
                ClientId = _nextClientId++,
                EndPointKey = endPointKey,
                Peer = new KcpPeer(conv, remoteEndPoint, _settings, SendRaw)
            };

            _clientsById.Add(session.ClientId, session);
            _clientsByEndPoint.Add(endPointKey, session);
            OnClientConnected?.Invoke(session.ClientId);
            return session;
        }

        private void UpdateClients(DateTimeOffset now)
        {
            foreach (ClientSession session in _clientsById.Values)
            {
                session.Peer.Update(now);

                while (session.Peer.TryReceive(out byte[] data))
                {
                    OnDataReceived?.Invoke(session.ClientId, data);
                }
            }
        }

        private void RemoveTimedOutClients(DateTimeOffset now)
        {
            if (_settings.DisconnectTimeout <= 0f)
            {
                return;
            }

            List<uint> timedOutClientIds = null;
            foreach (ClientSession session in _clientsById.Values)
            {
                double inactiveSeconds = (now - session.Peer.LastReceiveTime).TotalSeconds;
                if (inactiveSeconds < _settings.DisconnectTimeout)
                {
                    continue;
                }

                if (timedOutClientIds == null)
                {
                    timedOutClientIds = new List<uint>();
                }

                timedOutClientIds.Add(session.ClientId);
            }

            if (timedOutClientIds == null)
            {
                return;
            }

            foreach (uint clientId in timedOutClientIds)
            {
                RemoveClient(clientId);
            }
        }

        private void RemoveClient(uint clientId)
        {
            if (!_clientsById.TryGetValue(clientId, out ClientSession session))
            {
                return;
            }

            _clientsById.Remove(clientId);
            _clientsByEndPoint.Remove(session.EndPointKey);
            session.Peer.Dispose();
            OnClientDisconnected?.Invoke(clientId);
        }

        private void SendRaw(KcpPeer peer, byte[] data, int length)
        {
            if (!IsRunning || _udpServer == null || peer.RemoteEndPoint == null)
            {
                return;
            }

            _udpServer.Send(data, length, (IPEndPoint)peer.RemoteEndPoint);
        }

        private static uint ReadConv(byte[] datagram)
        {
            if (BitConverter.IsLittleEndian)
            {
                return BitConverter.ToUInt32(datagram, 0);
            }

            return BinaryPrimitives.ReadUInt32LittleEndian(datagram);
        }

        private void RaiseError(string message)
        {
            OnTransportError?.Invoke(message);
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(KcpServerTransport));
            }
        }
    }
}
