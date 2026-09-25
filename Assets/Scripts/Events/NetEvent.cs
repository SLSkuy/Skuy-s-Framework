namespace Events
{
    /// <summary>
    /// 网络通信事件类型
    /// </summary>
    public enum NetEvent : ushort
    {
        // DEBUG
        ERROR,
        CHAT_TEST,
        
        // 客户端连接
        RELIABLE_CONNECT_REQUEST,
        RELIABLE_CONNECT_RESPONSE,
        FAST_CONNECT_REQUEST,
        
        // 心跳/RTT
        HEART_BEAT_REQUEST,
        HEART_BEAT_RESPONSE,
        PING,
        PONG,

        // 房间玩家管理
        ROOM_JOIN_REQUEST,
        ROOM_JOIN_RESPONSE,
        ROOM_LEAVE_REQUEST,
        ROOM_PLAYER_LEAVE_NOTIFY,
        ROOM_PLAYER_JOINED_NOTIFY,
        ROOM_HOST_DISSOLVED_NOTIFY,
        
        // 状态同步
        PLAYER_INPUT,
        WORLD_SNAPSHOT,
    }
}
