using System;
using System.Buffers;
using System.Net;
using System.Net.Sockets.Kcp;

namespace Network
{
    /// <summary>
    /// 单一KCP连接抽象
    /// </summary>
    internal sealed class KcpSession : IKcpCallback, IDisposable
    {
        private const int BUFFER_SIZE = 1024 * 64;
        
        private readonly Action<KcpSession, byte[], int> _output;
        private readonly byte[] _receiveBuffer = new byte[BUFFER_SIZE];

        public uint Conv { get; }
        public EndPoint RemoteEndPoint { get; set; }
        public SimpleSegManager.Kcp Kcp { get; }
        public DateTimeOffset LastReceiveTime { get; private set; }

        public KcpSession(uint conv, EndPoint remoteEndPoint, TransportSettings settings, Action<KcpSession, byte[], int> output)
        {
            Conv = conv;
            RemoteEndPoint = remoteEndPoint;
            _output = output;
            LastReceiveTime = DateTimeOffset.UtcNow;

            Kcp = new SimpleSegManager.Kcp(conv, this);
            Kcp.SetMtu(settings.mtu);
            Kcp.WndSize(settings.sendWindow, settings.receiveWindow);
            Kcp.NoDelay(settings.noDelay, settings.updateInterval, settings.fastResend, settings.disableCongestionControl);
        }

        public void Input(byte[] data)
        {
            LastReceiveTime = DateTimeOffset.UtcNow;
            Kcp.Input(data);
        }

        public void Send(byte[] data)
        {
            if (data == null || data.Length == 0)
            {
                return;
            }

            Kcp.Send(data);
        }

        public bool TryReceive(out byte[] data)
        {
            data = null;

            while (true)
            {
                int length = Kcp.Recv(_receiveBuffer);
                if (length < 0)
                {
                    return false;
                }

                data = new byte[length];
                Buffer.BlockCopy(_receiveBuffer, 0, data, 0, length);
                return true;
            }
        }

        public void Update(DateTimeOffset now)
        {
            Kcp.Update(now);
        }

        public void Output(IMemoryOwner<byte> buffer, int avalidLength)
        {
            try
            {
                byte[] datagram = new byte[avalidLength];
                buffer.Memory.Span.Slice(0, avalidLength).CopyTo(datagram);
                _output?.Invoke(this, datagram, avalidLength);
            }
            finally
            {
                buffer.Dispose();
            }
        }

        public void Dispose()
        {
            Kcp?.Dispose();
        }
    }
}
