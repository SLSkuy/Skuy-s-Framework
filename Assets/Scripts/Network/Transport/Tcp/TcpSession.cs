using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Network
{
    /// <summary>
    /// 单一TCP连接抽象
    /// </summary>
    public sealed class TcpSession : IDisposable
    {
        private const int RECEIVE_BUFFER_SIZE = 1024 * 64;
        private const int MAX_MESSAGE_SIZE = 1024 * 512;
        
        public bool Connected => _socket is { Connected: true };
        public EndPoint RemoteEndPoint => _socket.RemoteEndPoint;
        public DateTimeOffset LastReceiveTime { get; private set; }

        private readonly ConcurrentQueue<byte[]> _sendQueue = new ConcurrentQueue<byte[]>();
        private readonly SemaphoreSlim _sendSignal = new SemaphoreSlim(0);

        private byte[] _receiveBuffer = new byte[RECEIVE_BUFFER_SIZE];
        private Socket _socket;
        private CancellationTokenSource _cts;
        private int _receiveCount;
        private int _disconnectRaised;
        private bool _isDisposed;

        public event Action<byte[]> OnMessageReceived;
        public event Action OnDisconnected;
        public event Action<string> OnError;

        public void Connect(string targetIp, short targetPort)
        {
            if (string.IsNullOrWhiteSpace(targetIp))
            {
                throw new ArgumentException("Server IP cannot be empty.", nameof(targetIp));
            }

            try
            {
                IPAddress[] addresses = Dns.GetHostAddresses(targetIp);
                if (addresses.Length == 0)
                {
                    throw new SocketException((int)SocketError.HostNotFound);
                }

                IPAddress address = SelectAddress(addresses);
                Socket socket = new Socket(address.AddressFamily, SocketType.Stream, ProtocolType.Tcp)
                {
                    NoDelay = true
                };

                socket.Connect(address, targetPort);
                Start(socket);
            }
            catch (Exception ex)
            {
                throw new Exception($"TCP connect failed: {ex.Message}", ex);
            }
        }

        public void Start(Socket socket)
        {
            ThrowIfDisposed();
            Disconnect(false);

            _socket = socket ?? throw new ArgumentNullException(nameof(socket));
            _cts = new CancellationTokenSource();
            _socket.NoDelay = true;
            _receiveCount = 0;
            _disconnectRaised = 0;
            LastReceiveTime = DateTimeOffset.UtcNow;

            _ = Task.Run(() => ReceiveLoop(_cts.Token));
            _ = Task.Run(() => SendLoop(_cts.Token));
        }

        public void EnqueueSend(byte[] data)
        {
            if (!Connected || data == null || data.Length == 0)
            {
                return;
            }

            if (data.Length > MAX_MESSAGE_SIZE)
            {
                RaiseError($"TCP message is too large: {data.Length}");
                return;
            }

            byte[] length = BitConverter.GetBytes(IPAddress.HostToNetworkOrder(data.Length));
            byte[] packet = new byte[length.Length + data.Length];
            Buffer.BlockCopy(length, 0, packet, 0, length.Length);
            Buffer.BlockCopy(data, 0, packet, length.Length, data.Length);

            _sendQueue.Enqueue(packet);
            ReleaseSendSignal();
        }

        public void Disconnect()
        {
            Disconnect(true);
        }

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            Disconnect(false);
            _sendSignal.Dispose();
            _isDisposed = true;
        }

        private async Task SendLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await _sendSignal.WaitAsync(token);

                    while (_sendQueue.TryDequeue(out byte[] data))
                    {
                        await SendAll(data, token);
                    }
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
                    RaiseError($"TCP send failed: {ex.Message}");
                    RaiseDisconnected();
                    break;
                }
                catch (Exception ex)
                {
                    RaiseError($"TCP send failed: {ex.Message}");
                    RaiseDisconnected();
                    break;
                }
            }
        }

        private async Task ReceiveLoop(CancellationToken token)
        {
            byte[] buffer = new byte[RECEIVE_BUFFER_SIZE];

            while (!token.IsCancellationRequested)
            {
                try
                {
                    int length = await _socket.ReceiveAsync(buffer, SocketFlags.None);
                    token.ThrowIfCancellationRequested();

                    if (length <= 0)
                    {
                        break;
                    }

                    if (!EnsureReceiveCapacity(_receiveCount + length))
                    {
                        RaiseError("TCP receive buffer overflow.");
                        break;
                    }

                    Buffer.BlockCopy(buffer, 0, _receiveBuffer, _receiveCount, length);
                    _receiveCount += length;
                    ProcessReceiveBuffer();
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
                    RaiseError($"TCP receive failed: {ex.Message}");
                    RaiseDisconnected();
                    return;
                }
                catch (Exception ex)
                {
                    RaiseError($"TCP receive failed: {ex.Message}");
                    RaiseDisconnected();
                    return;
                }
            }

            RaiseDisconnected();
        }

        private void ProcessReceiveBuffer()
        {
            int offset = 0;

            while (true)
            {
                if (_receiveCount - offset < sizeof(int))
                {
                    break;
                }

                int bodyLength = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(_receiveBuffer, offset));
                if (bodyLength <= 0 || bodyLength > MAX_MESSAGE_SIZE)
                {
                    RaiseError($"TCP invalid packet length: {bodyLength}");
                    Disconnect();
                    return;
                }

                if (_receiveCount - offset - sizeof(int) < bodyLength)
                {
                    break;
                }
                
                byte[] message = new byte[bodyLength];
                Buffer.BlockCopy(_receiveBuffer, offset + sizeof(int), message, 0, bodyLength);
                LastReceiveTime = DateTimeOffset.UtcNow;
                OnMessageReceived?.Invoke(message);
                offset += sizeof(int) + bodyLength;
            }

            if (offset <= 0)
            {
                return;
            }

            Buffer.BlockCopy(_receiveBuffer, offset, _receiveBuffer, 0, _receiveCount - offset);
            _receiveCount -= offset;
        }

        private async Task SendAll(byte[] data, CancellationToken token)
        {
            int sent = 0;
            while (sent < data.Length)
            {
                int count = await _socket.SendAsync(new ArraySegment<byte>(data, sent, data.Length - sent), SocketFlags.None);
                if (count <= 0)
                {
                    throw new SocketException((int)SocketError.ConnectionReset);
                }

                sent += count;
                token.ThrowIfCancellationRequested();
            }
        }

        private bool EnsureReceiveCapacity(int requiredLength)
        {
            if (requiredLength <= _receiveBuffer.Length)
            {
                return true;
            }

            int newSize = _receiveBuffer.Length;
            while (newSize < requiredLength && newSize < MAX_MESSAGE_SIZE + sizeof(int))
            {
                newSize *= 2;
            }

            if (newSize < requiredLength || newSize > MAX_MESSAGE_SIZE + sizeof(int))
            {
                return false;
            }

            Array.Resize(ref _receiveBuffer, newSize);
            return true;
        }

        private void Disconnect(bool raiseEvent)
        {
            _cts?.Cancel();
            ReleaseSendSignal();

            Socket socket = _socket;
            _socket = null;

            if (socket != null)
            {
                try
                {
                    if (socket.Connected)
                    {
                        socket.Shutdown(SocketShutdown.Both);
                    }
                }
                catch (SocketException)
                {
                }
                catch (ObjectDisposedException)
                {
                }

                socket.Close();
                socket.Dispose();
            }

            _cts?.Dispose();
            _cts = null;

            while (_sendQueue.TryDequeue(out _))
            {
            }

            if (raiseEvent)
            {
                RaiseDisconnected();
            }
        }

        private void RaiseDisconnected()
        {
            if (Interlocked.Exchange(ref _disconnectRaised, 1) == 0)
            {
                OnDisconnected?.Invoke();
            }
        }

        private void RaiseError(string message)
        {
            OnError?.Invoke(message);
        }

        private void ReleaseSendSignal()
        {
            try
            {
                _sendSignal.Release();
            }
            catch (ObjectDisposedException)
            {
            }
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
                throw new ObjectDisposedException(nameof(TcpSession));
            }
        }
    }
}
