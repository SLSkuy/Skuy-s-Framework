using Events;
using Google.Protobuf;

namespace Network
{
    /// <summary>
    /// 战局可用的可靠发送口，不暴露传输会话与连接表。
    /// </summary>
    public interface IMatchMessenger
    {
        void SendReliable(uint clientId, NetEvent evt, IMessage message);
        void BroadcastReliable(NetEvent evt, IMessage message);
    }
}
