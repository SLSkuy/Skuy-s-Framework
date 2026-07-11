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
            public uint ClientId;
            public TcpSession Session;
        }

        public TransportType Type => TransportType.TCP;
        public bool IsRunning { get; private set; }

        private readonly object _clientLock = new object();
        private readonly Dictionary<uint, ClientSession> _clientsById = new Dictionary<uint, ClientSession>();
        private readonly Dictionary<TcpSession, uint> _clientIdsBySession = new Dictionary<TcpSession, uint>();
        private readonly ConcurrentQueue<Action> _mainThreadEvents = new ConcurrentQueue<Action>();
        private Socket _listener;
        private CancellationTokenSource _cts;
        private uint _nextClientId = 1;
        private bool _isDisposed;

        public event Action<uint> OnClientConnected;
        public event Action<uint> OnClientDisconnected;
        public event Action<uint, byte[]> OnDataReceived;
        public event Action<string> OnTransportError;

        #region 暴露接口
        
        public void StartServer(int port)
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
                IsRunning = true;

                _ = Task.Run(() => AcceptLoop(_cts.Token));
            }
            catch (Exception ex)
            {
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
            while (_mainThreadEvents.TryDequeue(out Action action))
            {
                action?.Invoke();
            }
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

            List<TcpSession> sessions = GetSessionSnapshot();
            lock (_clientLock)
            {
                _clientsById.Clear();
                _clientIdsBySession.Clear();
            }

            foreach (TcpSession session in sessions)
            {
                UnbindSession(session);
                session.Dispose();
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

        private async Task AcceptLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    Socket socket = await _listener.AcceptAsync();
                    token.ThrowIfCancellationRequested();
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
                    if (!token.IsCancellationRequested)
                    {
                        RaiseError($"TCP server accept failed: {ex.Message}");
                    }
                }
                catch (Exception ex)
                {
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
            ClientSession clientSession;

            lock (_clientLock)
            {
                uint clientId = _nextClientId++;
                clientSession = new ClientSession
                {
                    ClientId = clientId,
                    Session = session
                };
                _clientsById.Add(clientId, clientSession);
                _clientIdsBySession.Add(session, clientId);
            }

            session.OnMessageReceived += data => HandleMessageReceived(session, data);
            session.OnDisconnected += () => HandleDisconnected(session);
            session.OnError += RaiseError;
            session.Start(socket);

            _mainThreadEvents.Enqueue(() => OnClientConnected?.Invoke(clientSession.ClientId));
        }

        private void HandleMessageReceived(TcpSession session, byte[] data)
        {
            if (!TryGetClientId(session, out uint clientId))
            {
                return;
            }

            _mainThreadEvents.Enqueue(() => OnDataReceived?.Invoke(clientId, data));
        }

        private void HandleDisconnected(TcpSession session)
        {
            RemoveClient(session, true);
        }

        private void RemoveClient(TcpSession session, bool raiseEvent)
        {
            if (session == null)
            {
                return;
            }

            uint clientId;
            lock (_clientLock)
            {
                if (!_clientIdsBySession.TryGetValue(session, out clientId))
                {
                    return;
                }

                _clientIdsBySession.Remove(session);
                _clientsById.Remove(clientId);
            }

            UnbindSession(session);
            session.Dispose();

            if (raiseEvent)
            {
                _mainThreadEvents.Enqueue(() => OnClientDisconnected?.Invoke(clientId));
            }
        }

        private void UnbindSession(TcpSession session)
        {
            session.OnError -= RaiseError;
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

        private bool TryGetClientId(TcpSession session, out uint clientId)
        {
            lock (_clientLock)
            {
                return _clientIdsBySession.TryGetValue(session, out clientId);
            }
        }

        private void RaiseError(string message)
        {
            _mainThreadEvents.Enqueue(() => OnTransportError?.Invoke(message));
        }

        private void ClearEvents()
        {
            while (_mainThreadEvents.TryDequeue(out _))
            {
            }
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
