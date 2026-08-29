using Google.Protobuf;

namespace Network
{
    /// <summary>
    /// 客户端业务消息处理器。同一事件可登记多个实例，按实例注销。
    /// </summary>
    public interface INetHandler<T> where T : IMessage
    {
        void Handle(T message);
    }
}
