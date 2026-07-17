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
        private readonly Action<KcpSession, byte[], int> _output;
        private readonly SimpleSegManager.Kcp _kcp;

        public uint Conv { get; }
        public EndPoint RemoteEndPoint { get; }
        public DateTimeOffset LastReceiveTime { get; private set; }

        public KcpSession(EndPoint remoteEndPoint, Action<KcpSession, byte[], int> output, uint conv = 0)
        {
            KcpTransportConfig config = KcpTransportConfig.Instance;
            Conv = conv > 0 ? conv : config.conv;
            RemoteEndPoint = remoteEndPoint;
            _output = output;
            LastReceiveTime = DateTimeOffset.UtcNow;

            _kcp = new SimpleSegManager.Kcp(Conv, this);
            _kcp.SetMtu(config.mtu);
            _kcp.WndSize(config.sendWindow, config.receiveWindow);
            _kcp.NoDelay(config.noDelay, config.updateInterval, config.fastResend, config.disableCongestionControl);
        }

        public int Input(byte[] data)
        {
            if (data == null || data.Length == 0)
            {
                return -1;
            }

            int result = _kcp.Input(data);
            if (result >= 0)
            {
                LastReceiveTime = DateTimeOffset.UtcNow;
            }

            return result;
        }

        public int Send(byte[] data)
        {
            if (data == null || data.Length == 0)
            {
                return -1;
            }

            return _kcp.Send(data);
        }

        public bool TryReceive(out byte[] data)
        {
            data = null;

            (IMemoryOwner<byte> buffer, int length) = _kcp.TryRecv();
            if (length < 0)
            {
                buffer?.Dispose();
                return false;
            }

            try
            {
                data = new byte[length];
                buffer.Memory.Span.Slice(0, length).CopyTo(data);
                return true;
            }
            finally
            {
                buffer?.Dispose();
            }
        }

        public void Update(DateTimeOffset now)
        {
            _kcp.Update(now);
        }

        public void Output(IMemoryOwner<byte> buffer, int avalidLength)
        {
            byte[] datagram = null;
            try
            {
                datagram = ArrayPool<byte>.Shared.Rent(avalidLength);
                buffer.Memory.Span.Slice(0, avalidLength).CopyTo(datagram);
                _output?.Invoke(this, datagram, avalidLength);
            }
            finally
            {
                if (datagram != null)
                {
                    ArrayPool<byte>.Shared.Return(datagram);
                }
                buffer.Dispose();
            }
        }

        public void Dispose()
        {
            _kcp?.Dispose();
        }
    }
}
