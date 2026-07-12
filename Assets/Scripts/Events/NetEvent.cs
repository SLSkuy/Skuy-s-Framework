namespace Network
{
    /// <summary>
    /// 网络通信事件类型
    /// </summary>
    public enum NetEvent : ushort
    {
        ERROR,
        RELIABLE_CONNECT_REQUEST,
        RELIABLE_CONNECT_RESPONSE,
        FAST_CONNECT_REQUEST,
        PING,
        PONG,
        CHAT_TEST,
    }
}
