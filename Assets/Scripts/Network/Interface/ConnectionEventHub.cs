using System;

namespace Network
{
    /// <summary>
    /// 可手动触发的连接移除通知，供传输适配与测试使用。
    /// </summary>
    public sealed class ConnectionEventHub : IConnectionEvents
    {
        #region 事件
        public event Action<uint> ClientRemoved;
        #endregion

        /// <summary>
        /// 通知连接已被彻底移除。
        /// </summary>
        public void NotifyRemoved(uint clientId)
        {
            ClientRemoved?.Invoke(clientId);
        }
    }
}
