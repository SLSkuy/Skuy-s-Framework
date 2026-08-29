using Google.Protobuf;

namespace Network
{
    /// <summary>
    /// 服务端业务消息处理器。senderId 为连接标识，不是玩家标识。
    /// </summary>
    public interface IServerNetHandler<T> where T : IMessage
    {
        void Handle(uint senderId, T message);
    }
}
