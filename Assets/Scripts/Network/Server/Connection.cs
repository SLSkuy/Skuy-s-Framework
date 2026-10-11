namespace Network
{
    /// <summary>
    /// 客户端连接会话抽象，ID与token互相索引
    /// 可靠连接/快速连接指向同一个连接会话对象
    /// </summary>
    public class Connection
    {
        /// <summary>
        /// 连接ID
        /// </summary>
        public readonly uint Id;
        
        /// <summary>
        /// 连接Token标识
        /// </summary>
        public readonly ulong Token;
        
        /// <summary>
        /// 可靠连接会话ID
        /// </summary>
        public uint ReliableSessionId;
        
        /// <summary>
        /// 快速连接会话ID
        /// </summary>
        public uint FastSessionId;
        
        /// <summary>断连时间戳，-1表示已连接，>=0表示断连时刻</summary>
        public float DisconnectTime;

        #region 属性

        /// <summary>
        /// 网络延迟
        /// </summary>
        public float RTT;

        public bool IsConnected => ReliableSessionId != 0 || FastSessionId != 0;

        #endregion
        
        public Connection(uint id, ulong token)
        {
            Id = id;
            Token = token;
            ReliableSessionId = 0;
            FastSessionId = 0;
            DisconnectTime = -1f;
            RTT = 0;
        }
    }
}