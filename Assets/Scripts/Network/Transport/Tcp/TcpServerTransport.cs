using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Network
{
    /// <summary>
    /// TCP服务端传输封装
    /// </summary>
    public sealed class TcpServerTransport : IServerTransport
    {
        private sealed class ClientSession
        {
            public TcpServerTransport Owner;
            public uint SessionId;
            public TcpSession Session;

            public void Bind()
            {
                Session.OnMessageReceived += HandleMessageReceived;
                Session.OnDisconnected += HandleDisconnected;
                Session.OnError += HandleError;
            }

            public void Unbind()
            {
                Session.OnMessageReceived -= HandleMessageReceived;
                Session.OnDisconnected -= HandleDisconnected;
                Session.OnError -= HandleError;
            }

            private void HandleMessageReceived(byte[] data)
            {
                Owner.EnqueueMessage(SessionId, data);
            }

            private void HandleDisconnected()
            {
                Owner.RemoveClient(SessionId, true);
            }

            private void HandleError(string message)
            {
                Owner.RaiseError(message);
            }
        }

        private struct ClientMessage
        {
            public uint ClientId;
            public byte[] Data;
        }

        private struct TransportError
        {
            public string Message;
        }

        public TransportType Type => TransportType.TCP;
        public short Port { get; private set; }
        public bool IsRunning { get; private set; }
        
        private readonly TransportSettings _settings;
        private readonly object _clientLock = new object();
        private readonly Dictionary<uint, ClientSession> _clientsById = new Dictionary<uint, ClientSession>();
        private readonly ConcurrentQueue<uint> _connectedSessions = new ConcurrentQueue<uint>();
        private readonly ConcurrentQueue<uint> _disconnectedSessions = new ConcurrentQueue<uint>();
        private readonly ConcurrentQueue<ClientMessage> _receivedMessages = new ConcurrentQueue<ClientMessage>();
        private readonly ConcurrentQueue<TransportError> _errors = new ConcurrentQueue<TransportError>();
        private readonly List<uint> _timeOutSessionIds = new List<uint>();
        private Socket _listener;
        private CancellationTokenSource _cts;
        private uint _nextClientId = 1;
        private bool _isDisposed;

        public event Action<uint> OnClientConnected;
        public event Action<uint> OnClientDisconnected;
        public event Action<uint, byte[]> OnDataReceived;
        public event Action<string> OnTransportError;

        public TcpServerTransport(TransportSettings settings)
        {
            _settings = settings ?? TransportSettings.Default;
        }

        #region 暴露接口
        
        public void StartServer(short port)
        {
            ThrowIfDisposed();
            Stop();

            try
            {
                _cts = new CancellationTokenSource();
                _listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
                {
                    NoDelay = true
                };
                _listener.Bind(new IPEndPoint(IPAddress.Any, port));
                _listener.Listen(128);
                Port = port;
                IsRunning = true;

                _ = Task.Run(() => AcceptLoop(_cts.Token));
            }
            catch (Exception ex)
            {
                // 同步抛出错误消息
                string message = $"TCP server start failed: {ex.Message}";
                Stop();
                OnTransportError?.Invoke(message);
            }
        }

        public void Send(uint clientId, byte[] data)
        {
            if (!IsRunning)
            {
                RaiseError("TCP server is not running.");
                return;
            }

            TcpSession session = GetSession(clientId);
            if (session == null)
            {
                RaiseError($"TCP server client not found: {clientId}");
                return;
            }

            session.EnqueueSend(data);
        }

        public void Broadcast(byte[] data)
        {
            if (!IsRunning)
            {
                RaiseError("TCP server is not running.");
                return;
            }

            foreach (TcpSession session in GetSessionSnapshot())
            {
                session.EnqueueSend(data);
            }
        }

        public void Update(float deltaTime)
        {
            while (_connectedSessions.TryDequeue(out uint connectedClientId))
            {
                OnClientConnected?.Invoke(connectedClientId);
            }

            while (_disconnectedSessions.TryDequeue(out uint disconnectedClientId))
            {
                OnClientDisconnected?.Invoke(disconnectedClientId);
            }

            while (_receivedMessages.TryDequeue(out ClientMessage message))
            {
                OnDataReceived?.Invoke(message.ClientId, message.Data);
            }

            while (_errors.TryDequeue(out TransportError error))
            {
                OnTransportError?.Invoke(error.Message);
            }
            
            DateTimeOffset now = DateTimeOffset.UtcNow;
            RemoveTimedOutClients(now);
        }

        public void Stop()
        {
            IsRunning = false;

            _cts?.Cancel();

            try
            {
                _listener?.Close();
            }
            catch (ObjectDisposedException)
            {
            }

            _listener?.Dispose();
            _listener = null;

            List<ClientSession> sessions = RemoveAllClients();
            foreach (ClientSession session in sessions)
            {
                session.Unbind();
                session.Session.Dispose();
            }

            _cts?.Dispose();
            _cts = null;
            ClearEvents();
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

        private void RemoveTimedOutClients(DateTimeOffset now)
        {
            if (_settings.disconnectTimeout <= 0f)
            {
                return;
            }
            
            lock (_clientLock)
            {
                foreach (var session in _clientsById.Values)
                {
                    double inactiveSeconds = (now - session.Session.LastReceiveTime).TotalSeconds;
                    if (inactiveSeconds < _settings.disconnectTimeout)
                    {
                        continue;
                    }
                    _timeOutSessionIds.Add(session.SessionId);    
                }
            }
            
            foreach (var session in _timeOutSessionIds)
            {
                RemoveClient(session, true);
            }
            _timeOutSessionIds.Clear();
        }

        private async Task AcceptLoop(CancellationToken token)
        {
            while (IsRunning && !token.IsCancellationRequested)
            {
                try
                {
                    Socket listener = _listener;
                    if (listener == null)
                    {
                        break;
                    }

                    Socket socket = await listener.AcceptAsync();
                    token.ThrowIfCancellationRequested();

                    if (!IsRunning)
                    {
                        socket.Dispose();
                        break;
                    }

                    AddClient(socket);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (SocketException ex)
                {
                    if (!IsRunning || token.IsCancellationRequested)
                    {
                        break;
                    }

                    if (_listener == null)
                    {
                        break;
                    }

                    if (!token.IsCancellationRequested)
                    {
                        RaiseError($"TCP server accept failed: {ex.Message}");
                    }
                }
                catch (Exception ex)
                {
                    if (!IsRunning || token.IsCancellationRequested)
                    {
                        break;
                    }

                    if (_listener == null)
                    {
                        break;
                    }

                    if (!token.IsCancellationRequested)
                    {
                        RaiseError($"TCP server accept failed: {ex.Message}");
                    }
                }
            }
        }

        private void AddClient(Socket socket)
        {
            TcpSession session = new TcpSession();
            uint clientId = 0;
            bool registered = false;

            try
            {
                lock (_clientLock)
                {
                    if (!IsRunning)
                    {
                        socket.Dispose();
                        session.Dispose();
                        return;
                    }

                    clientId = _nextClientId++;
                    var clientSession = new ClientSession
                    {
                        Owner = this,
                        SessionId = clientId,
                        Session = session
                    };
                    _clientsById.Add(clientId, clientSession);
                    registered = true;
                    clientSession.Bind();
                    session.Start(socket);
                }

                _connectedSessions.Enqueue(clientId);
            }
            catch (Exception ex)
            {
                if (registered)
                {
                    lock (_clientLock)
                    {
                        _clientsById.Remove(clientId);
                    }
                }

                session.Dispose();
                RaiseError($"TCP server client start failed: {ex.Message}");
            }
        }

        private void EnqueueMessage(uint clientId, byte[] data)
        {
            _receivedMessages.Enqueue(new ClientMessage
            {
                ClientId = clientId,
                Data = data
            });
        }

        private void RemoveClient(uint sessionId, bool raiseEvent)
        {
            ClientSession session;
            lock (_clientLock)
            {
                if (!_clientsById.Remove(sessionId, out session))
                {
                    return;
                }
            }

            session.Unbind();
            session.Session.Dispose();

            if (raiseEvent)
            {
                _disconnectedSessions.Enqueue(sessionId);
            }
        }

        private TcpSession GetSession(uint clientId)
        {
            lock (_clientLock)
            {
                return _clientsById.TryGetValue(clientId, out ClientSession session) ? session.Session : null;
            }
        }

        private List<TcpSession> GetSessionSnapshot()
        {
            lock (_clientLock)
            {
                List<TcpSession> sessions = new List<TcpSession>(_clientsById.Count);
                foreach (ClientSession session in _clientsById.Values)
                {
                    sessions.Add(session.Session);
                }

                return sessions;
            }
        }

        private List<ClientSession> RemoveAllClients()
        {
            lock (_clientLock)
            {
                List<ClientSession> sessions = new List<ClientSession>(_clientsById.Values);
                _clientsById.Clear();
                return sessions;
            }
        }

        private void RaiseError(string message)
        {
            _errors.Enqueue(new TransportError
            {
                Message = message
            });
        }

        private void ClearEvents()
        {
            while (_connectedSessions.TryDequeue(out _)) { }
            while (_disconnectedSessions.TryDequeue(out _)) { }
            while (_receivedMessages.TryDequeue(out _)) { }
            while (_errors.TryDequeue(out _)) { }
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(TcpServerTransport));
            }
        }

        #endregion
    }
}
