namespace Events
{
    /// <summary>
    /// 网络通信事件类型
    /// </summary>
    public enum NetEvent : ushort
    {
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
        GAME_JOIN_REQUEST,
        GAME_JOIN_RESPONSE,
        GAME_LEAVE_REQUEST,
        GAME_LEAVE_NOTIFY,
        GAME_PLAYER_JOINED,
        
        // 状态同步
        PLAYER_INPUT,
        WORLD_SNAPSHOT,
        ENTITY_SPAWN,
        ENTITY_DESPAWN,
    }
}
