using Events;
using Google.Protobuf;

namespace Network
{
    /// <summary>
    /// 将可靠发送转发到 <see cref="NetServer"/>，供组合根注入战局。
    /// </summary>
    public sealed class NetServerMatchMessenger : IMatchMessenger
    {
        private readonly NetServer _server;

        public NetServerMatchMessenger(NetServer server)
        {
            _server = server;
        }

        public void SendReliable(uint clientId, NetEvent evt, IMessage message)
        {
            _server.SendReliable(clientId, evt, message);
        }

        public void BroadcastReliable(NetEvent evt, IMessage message)
        {
            _server.BroadcastReliable(evt, message);
        }
    }
}
