using System;
using System.Collections.Generic;

namespace Network
{
    /// <summary>
    /// 客户端连接抽象管理
    /// </summary>
    public sealed class ConnectionManager
    {
        public int ClientCount => _clientsById.Count;

        private uint _nextClientId = 1;
        private readonly Dictionary<uint, Connection> _clientsById = new Dictionary<uint, Connection>();
        private readonly Dictionary<ulong, Connection> _clientsByToken = new Dictionary<ulong, Connection>();
        private readonly Dictionary<uint, uint> _clientIdsByReliableSessionId = new Dictionary<uint, uint>();
        private readonly Dictionary<uint, uint> _clientIdsByFastSessionId = new Dictionary<uint, uint>();
        private readonly List<uint> _clientToRemove = new List<uint>();

        #region 事件
        public event Action<uint> OnClientRemoved;
        #endregion
        
        #region 客户端管理

        /// <summary>
        /// 注册新客户端连接，生成唯一Token
        /// </summary>
        public Connection AddClient(uint reliableSessionId = 0)
        {
            if (reliableSessionId != 0 &&
                _clientIdsByReliableSessionId.TryGetValue(reliableSessionId, out uint existingClientId) &&
                _clientsById.TryGetValue(existingClientId, out Connection existingClient))
            {
                return existingClient;
            }

            ulong token = CreateUniqueToken();
            Connection connection = new Connection(_nextClientId, token)
            {
                ReliableSessionId = reliableSessionId
            };

            _clientsById[_nextClientId] = connection;
            _clientsByToken[token] = connection;
            if (reliableSessionId != 0)
            {
                _clientIdsByReliableSessionId[reliableSessionId] = _nextClientId;
            }

            _nextClientId++;
            return connection;
        }

        public bool BindFastSession(ulong token, uint fastSessionId, out uint oldFastSessionId)
        {
            oldFastSessionId = 0;
            if (!TryGetClient(token, out var client))
            {
                return false;
            }

            oldFastSessionId = client.FastSessionId;
            if (oldFastSessionId != 0)
            {
                _clientIdsByFastSessionId.Remove(oldFastSessionId);
            }

            client.FastSessionId = fastSessionId;
            client.DisconnectTime = -1f;
            _clientIdsByFastSessionId[fastSessionId] = client.Id;
            return true;
        }

        /// <summary>
        /// 将新的TCP session重新绑定到已有客户端（旧客户端恢复/顶号）
        /// </summary>
        public bool RebindReliableSession(ulong token, uint reliableSessionId, out uint oldReliableSessionId)
        {
            oldReliableSessionId = 0;
            if (!_clientsByToken.TryGetValue(token, out var client))
            {
                return false;
            }

            // 如果已有旧TCP session，先解除
            oldReliableSessionId = client.ReliableSessionId;
            if (oldReliableSessionId != 0)
            {
                _clientIdsByReliableSessionId.Remove(oldReliableSessionId);
            }

            client.ReliableSessionId = reliableSessionId;
            client.DisconnectTime = -1f;
            _clientIdsByReliableSessionId[reliableSessionId] = client.Id;
            return true;
        }

        public bool UnBindReliableSession(uint reliableSessionId, out uint clientId, float currentTime = 0f)
        {
            if (!_clientIdsByReliableSessionId.Remove(reliableSessionId, out clientId))
            {
                clientId = 0;
                return false;
            }

            if (_clientsById.TryGetValue(clientId, out Connection client) && client.ReliableSessionId == reliableSessionId)
            {
                client.ReliableSessionId = 0;
            }

            // 两个session都断开时，记录断连时间（宽限期内保留client等待重连）
            if (!CheckClientConnected(clientId) && _clientsById.TryGetValue(clientId, out Connection disconnected))
            {
                disconnected.DisconnectTime = currentTime > 0f ? currentTime : -1f;
            }

            return true;
        }

        public bool UnbindFastSession(uint fastSessionId, out uint clientId, float currentTime = 0f)
        {
            if (!_clientIdsByFastSessionId.Remove(fastSessionId, out clientId))
            {
                clientId = 0;
                return false;
            }

            if (_clientsById.TryGetValue(clientId, out Connection client) && client.FastSessionId == fastSessionId)
            {
                client.FastSessionId = 0;
            }

            // 两个session都断开时，记录断连时间（宽限期内保留client等待重连）
            if (!CheckClientConnected(clientId) && _clientsById.TryGetValue(clientId, out Connection disconnected))
            {
                disconnected.DisconnectTime = currentTime > 0f ? currentTime : -1f;
            }

            return true;
        }

        /// <summary>
        /// 检测当前Client是否还存在某一个连接，若都不存在，则Client彻底断连
        /// </summary>
        public bool CheckClientConnected(uint clientId)
        {
            if (TryGetClient(clientId, out var client))
            {
                return (client.ReliableSessionId != 0 || client.FastSessionId != 0);
            }

            return false;
        }

        /// <summary>
        /// 移除客户端
        /// </summary>
        public bool RemoveClient(uint clientId)
        {
            if (!_clientsById.Remove(clientId, out Connection client))
            {
                return false;
            }

            _clientsByToken.Remove(client.Token);
            if (client.ReliableSessionId != 0)
            {
                _clientIdsByReliableSessionId.Remove(client.ReliableSessionId);
            }

            if (client.FastSessionId != 0)
            {
                _clientIdsByFastSessionId.Remove(client.FastSessionId);
            }

            OnClientRemoved?.Invoke(clientId);
            return true;
        }

        /// <summary>
        /// 通过Token移除客户端
        /// </summary>
        public bool RemoveClient(ulong token)
        {
            if (!_clientsByToken.TryGetValue(token, out Connection client))
            {
                return false;
            }

            _clientsById.Remove(client.Id);
            _clientsByToken.Remove(token);
            if (client.ReliableSessionId != 0)
            {
                _clientIdsByReliableSessionId.Remove(client.ReliableSessionId);
            }

            if (client.FastSessionId != 0)
            {
                _clientIdsByFastSessionId.Remove(client.FastSessionId);
            }

            OnClientRemoved?.Invoke(client.Id);
            return true;
        }

        #endregion

        #region 客户端查询

        public bool TryGetClient(uint clientId, out Connection connection)
        {
            return _clientsById.TryGetValue(clientId, out connection);
        }

        public bool TryGetClient(ulong token, out Connection connection)
        {
            return _clientsByToken.TryGetValue(token, out connection);
        }

        public bool TryGetClientId(ulong token, out uint clientId)
        {
            if (_clientsByToken.TryGetValue(token, out Connection client))
            {
                clientId = client.Id;
                return true;
            }

            clientId = 0;
            return false;
        }

        public bool TryGetToken(uint clientId, out ulong token)
        {
            if (_clientsById.TryGetValue(clientId, out Connection client))
            {
                token = client.Token;
                return true;
            }

            token = 0;
            return false;
        }

        public bool TryGetClientId(TransportType transportType, uint transportSessionId, out uint clientId)
        {
            Dictionary<uint, uint> map = transportType == TransportType.TCP
                ? _clientIdsByReliableSessionId
                : _clientIdsByFastSessionId;

            return map.TryGetValue(transportSessionId, out clientId);
        }

        public bool TryGetReliableSessionId(uint clientId, out uint reliableSessionId)
        {
            if (_clientsById.TryGetValue(clientId, out Connection client) && client.ReliableSessionId != 0)
            {
                reliableSessionId = client.ReliableSessionId;
                return true;
            }

            reliableSessionId = 0;
            return false;
        }

        public bool TryGetFastSessionId(uint clientId, out uint fastSessionId)
        {
            if (_clientsById.TryGetValue(clientId, out Connection client) && client.FastSessionId != 0)
            {
                fastSessionId = client.FastSessionId;
                return true;
            }

            fastSessionId = 0;
            return false;
        }

        public IEnumerable<Connection> GetAllConnections()
        {
            return _clientsById.Values;
        }

        #endregion

        #region 清理

        /// <summary>
        /// 清理超出宽限期的断连客户端
        /// </summary>
        /// <param name="currentTime">当前时间（通常为Time.time）</param>
        /// <param name="gracePeriodSeconds">宽限期（秒），断连超过此时间后彻底移除</param>
        public void CleanupDisconnectedClients(float currentTime, float gracePeriodSeconds)
        {
            if (gracePeriodSeconds <= 0f) return;

            foreach (var kv in _clientsById)
            {
                Connection connection = kv.Value;
                if (connection.DisconnectTime < 0f) continue;
                if (currentTime - connection.DisconnectTime >= gracePeriodSeconds)
                {
                    _clientToRemove.Add(connection.Id);
                }
            }

            // 移除超时的客户端
            if (_clientToRemove.Count == 0) return;
            foreach (uint id in _clientToRemove)
            {
                RemoveClient(id);
            }
            _clientToRemove.Clear();
        }

        /// <summary>
        /// 更新客户端 RTT（由服务端 Ping 测量）
        /// </summary>
        public void UpdateRTT(uint clientId, float rtt)
        {
            if (_clientsById.TryGetValue(clientId, out Connection client))
            {
                client.RTT = rtt;
            }
        }

        public void Clear()
        {
            uint[] removedClientIds = new uint[_clientsById.Count];
            _clientsById.Keys.CopyTo(removedClientIds, 0);
            _clientsById.Clear();
            _clientsByToken.Clear();
            _clientIdsByReliableSessionId.Clear();
            _clientIdsByFastSessionId.Clear();
            _nextClientId = 1;

            foreach (uint clientId in removedClientIds)
            {
                OnClientRemoved?.Invoke(clientId);
            }
        }

        #endregion

        #region Token生成

        /// <summary>
        /// 创建唯一连接Token
        /// </summary>
        private ulong CreateUniqueToken()
        {
            ulong token;
            do
            {
                token = CreateToken();
            }
            while (token == 0 || _clientsByToken.ContainsKey(token));

            return token;
        }

        private static ulong CreateToken()
        {
            byte[] bytes = new byte[sizeof(ulong)];
            using (System.Security.Cryptography.RandomNumberGenerator rng =
                   System.Security.Cryptography.RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }

            return BitConverter.ToUInt64(bytes, 0);
        }

        #endregion
    }
}
