using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace Network
{
    /// <summary>
    /// KCP服务端传输封装
    /// </summary>
    public sealed class KcpServerTransport : IServerTransport
    {
        private sealed class ClientSession
        {
            public uint ClientId;
            public KcpSession Session;
            public string EndPointKey;
        }
        
        private const int KCP_HEADER_SIZE = 24;
        
        public TransportType Type => TransportType.KCP;
        public bool IsRunning { get; private set; }

        private readonly TransportSettings _settings;
        private readonly Dictionary<uint, ClientSession> _clientsById = new Dictionary<uint, ClientSession>();
        private readonly Dictionary<string, ClientSession> _clientsByEndPoint = new Dictionary<string, ClientSession>();
        private UdpClient _udpServer;
        private uint _nextClientId = 1;
        private bool _isDisposed;

        public event Action<uint> OnClientConnected;
        public event Action<uint> OnClientDisconnected;
        public event Action<uint, byte[]> OnDataReceived;
        public event Action<string> OnTransportError;

        public KcpServerTransport(TransportSettings settings)
        {
            _settings = settings ?? TransportSettings.Default;
        }

        #region 暴露接口

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
                int result = session.Session.Send(data);
                if (result < 0)
                {
                    RaiseError($"KCP server send failed for client {clientId} with code: {result}");
                }
            }
        }

        public void Broadcast(byte[] data)
        {
            foreach (ClientSession session in _clientsById.Values)
            {
                int result = session.Session.Send(data);
                if (result < 0)
                {
                    RaiseError($"KCP server broadcast failed for client {session.ClientId} with code: {result}");
                }
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
                session.Session.Dispose();
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

        #endregion

        #region 内部管理方法

        
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

                int result = session.Session.Input(datagram);
                if (result < 0)
                {
                    RaiseError($"KCP server input failed for client {session.ClientId} with code: {result}");
                }
            }
        }

        private ClientSession GetOrCreateSession(IPEndPoint remoteEndPoint, byte[] datagram)
        {
            string endPointKey = remoteEndPoint.ToString();
            if (_clientsByEndPoint.TryGetValue(endPointKey, out ClientSession session))
            {
                return session;
            }

            if (datagram == null || datagram.Length < KCP_HEADER_SIZE)
            {
                return null;
            }

            uint conv = ReadConv(datagram);
            session = new ClientSession
            {
                ClientId = _nextClientId++,
                EndPointKey = endPointKey,
                Session = new KcpSession(conv, remoteEndPoint, _settings, SendRaw)
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
                session.Session.Update(now);

                while (session.Session.TryReceive(out byte[] data))
                {
                    OnDataReceived?.Invoke(session.ClientId, data);
                }
            }
        }

        private void RemoveTimedOutClients(DateTimeOffset now)
        {
            if (_settings.disconnectTimeout <= 0f)
            {
                return;
            }

            List<uint> timedOutClientIds = null;
            foreach (ClientSession session in _clientsById.Values)
            {
                double inactiveSeconds = (now - session.Session.LastReceiveTime).TotalSeconds;
                if (inactiveSeconds < _settings.disconnectTimeout)
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
            session.Session.Dispose();
            OnClientDisconnected?.Invoke(clientId);
        }

        private void SendRaw(KcpSession session, byte[] data, int length)
        {
            if (!IsRunning || _udpServer == null || session.RemoteEndPoint == null)
            {
                return;
            }

            _udpServer.Send(data, length, (IPEndPoint)session.RemoteEndPoint);
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

        #endregion
    }
}
