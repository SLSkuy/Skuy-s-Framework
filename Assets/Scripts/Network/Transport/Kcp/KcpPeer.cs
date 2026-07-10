using System;
using System.Buffers;
using System.Net;
using System.Net.Sockets.Kcp;

namespace Network
{
    internal sealed class KcpPeer : IKcpCallback, IDisposable
    {
        private readonly Action<KcpPeer, byte[], int> _output;
        private readonly byte[] _receiveBuffer = new byte[64 * 1024];

        public uint Conv { get; }
        public EndPoint RemoteEndPoint { get; set; }
        public SimpleSegManager.Kcp Kcp { get; }
        public DateTimeOffset LastReceiveTime { get; private set; }

        public KcpPeer(uint conv, EndPoint remoteEndPoint, TransportSettings settings, Action<KcpPeer, byte[], int> output)
        {
            Conv = conv;
            RemoteEndPoint = remoteEndPoint;
            _output = output;
            LastReceiveTime = DateTimeOffset.UtcNow;

            Kcp = new SimpleSegManager.Kcp(conv, this);
            Kcp.SetMtu(settings.Mtu);
            Kcp.WndSize(settings.SendWindow, settings.ReceiveWindow);
            Kcp.NoDelay(settings.NoDelay, settings.UpdateInterval, settings.FastResend, settings.DisableCongestionControl);
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
