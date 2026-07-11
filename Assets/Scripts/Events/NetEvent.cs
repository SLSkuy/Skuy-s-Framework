namespace Network
{
    /// <summary>
    /// 网络通信事件类型
    /// </summary>
    public enum NetEvent : ushort
    {
        ERROR,
        START_REQUEST,
        START_RESPONSE,
        KCP_CONNECT_REQUEST,
        PING,
        PONG,
        CHAT_TEST,
    }
}
