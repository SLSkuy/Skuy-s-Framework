using System;

namespace Network
{
    /// <summary>
    /// 将 <see cref="NetServer.OnClientRemoved"/> 翻译为 <see cref="IConnectionEvents"/>。
    /// </summary>
    public sealed class NetServerConnectionEvents : IConnectionEvents
    {
        private readonly NetServer _server;

        #region 事件
        public event Action<uint> ClientRemoved;
        #endregion

        public NetServerConnectionEvents(NetServer server)
        {
            _server = server;
            _server.OnClientRemoved += HandleRemoved;
        }

        /// <summary>
        /// 解除对传输服务端的订阅。
        /// </summary>
        public void Dispose()
        {
            if (_server != null)
            {
                _server.OnClientRemoved -= HandleRemoved;
            }
        }

        private void HandleRemoved(uint clientId)
        {
            ClientRemoved?.Invoke(clientId);
        }
    }
}
