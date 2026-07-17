using System;
using System.Collections.Generic;
using UnityEngine;

namespace Network
{
    /// <summary>
    /// 客户端抽象管理，控制客户端在服务端的连接抽象
    /// </summary>
    public sealed class ClientManager
    {
        /// <summary>
        /// 客户端抽象
        /// </summary>
        public struct Client
        {
            public readonly uint Id;
            public readonly ulong Token;
            public uint ReliableSessionId;
            public uint FastSessionId;
            /// <summary>断连时间戳，-1表示已连接，>=0表示断连时刻</summary>
            public float DisconnectTime;

            public bool IsConnected => ReliableSessionId != 0 || FastSessionId != 0;

            public Client(uint id, ulong token)
            {
                Id = id;
                Token = token;
                ReliableSessionId = 0;
                FastSessionId = 0;
                DisconnectTime = -1f;
            }
        }

        public int ClientCount
        {
            get { lock (_lock) { return _clientsById.Count; } }
        }

        private readonly object _lock = new object();
        private uint _nextClientId = 1;
        private readonly Dictionary<uint, Client> _clientsById = new Dictionary<uint, Client>();
        private readonly Dictionary<ulong, Client> _clientsByToken = new Dictionary<ulong, Client>();
        private readonly Dictionary<uint, uint> _clientIdsByReliableSessionId = new Dictionary<uint, uint>();
        private readonly Dictionary<uint, uint> _clientIdsByFastSessionId = new Dictionary<uint, uint>();

        #region 客户端管理

        /// <summary>
        /// 注册新客户端连接，生成唯一Token
        /// </summary>
        public Client AddClient(uint reliableSessionId = 0)
        {
            lock (_lock)
            {
                if (reliableSessionId != 0 && 
                    _clientIdsByReliableSessionId.TryGetValue(reliableSessionId, out uint existingClientId) &&
                    _clientsById.TryGetValue(existingClientId, out Client existingClient))
                {
                    return existingClient;
                }

                ulong token = CreateUniqueToken();
                Client client = new Client(_nextClientId, token)
                {
                    ReliableSessionId = reliableSessionId
                };

                _clientsById[_nextClientId] = client;
                _clientsByToken[token] = client;
                if (reliableSessionId != 0)
                {
                    _clientIdsByReliableSessionId[reliableSessionId] = _nextClientId;
                }

                _nextClientId++;
                return client;
            }
        }

        public bool BindFastSession(ulong token, uint fastSessionId, out uint oldFastSessionId)
        {
            oldFastSessionId = 0;
            lock (_lock)
            {
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
                _clientsById[client.Id] = client;
                _clientsByToken[token] = client;
                _clientIdsByFastSessionId[fastSessionId] = client.Id;
                return true;
            }
        }

        /// <summary>
        /// 将新的TCP session重新绑定到已有客户端（旧客户端恢复/顶号）
        /// </summary>
        public bool RebindReliableSession(ulong token, uint reliableSessionId, out uint oldReliableSessionId)
        {
            oldReliableSessionId = 0;
            lock (_lock)
            {
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
                _clientsById[client.Id] = client;
                _clientsByToken[token] = client;
                _clientIdsByReliableSessionId[reliableSessionId] = client.Id;
                return true;
            }
        }

        public bool UnBindReliableSession(uint reliableSessionId, out uint clientId, float currentTime = 0f)
        {
            lock (_lock)
            {
                if (!_clientIdsByReliableSessionId.Remove(reliableSessionId, out clientId))
                {
                    clientId = 0;
                    return false;
                }

                if (_clientsById.TryGetValue(clientId, out Client client) && client.ReliableSessionId == reliableSessionId)
                {
                    client.ReliableSessionId = 0;
                    _clientsById[clientId] = client;
                    _clientsByToken[client.Token] = client;
                }

                // 两个session都断开时，记录断连时间（宽限期内保留client等待重连）
                if (!CheckClientConnected(clientId) && _clientsById.TryGetValue(clientId, out Client disconnected))
                {
                    disconnected.DisconnectTime = currentTime > 0f ? currentTime : -1f;
                    _clientsById[clientId] = disconnected;
                    _clientsByToken[disconnected.Token] = disconnected;
                }

                return true;
            }
        }

        public bool UnbindFastSession(uint fastSessionId, out uint clientId, float currentTime = 0f)
        {
            lock (_lock)
            {
                if (!_clientIdsByFastSessionId.Remove(fastSessionId, out clientId))
                {
                    clientId = 0;
                    return false;
                }

                if (_clientsById.TryGetValue(clientId, out Client client) && client.FastSessionId == fastSessionId)
                {
                    client.FastSessionId = 0;
                    _clientsById[clientId] = client;
                    _clientsByToken[client.Token] = client;
                }
                
                // 两个session都断开时，记录断连时间（宽限期内保留client等待重连）
                if (!CheckClientConnected(clientId) && _clientsById.TryGetValue(clientId, out Client disconnected))
                {
                    disconnected.DisconnectTime = currentTime > 0f ? currentTime : -1f;
                    _clientsById[clientId] = disconnected;
                    _clientsByToken[disconnected.Token] = disconnected;
                }

                return true;
            }
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
            lock (_lock)
            {
                if (!_clientsById.Remove(clientId, out Client client))
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

                return true;
            }
        }

        /// <summary>
        /// 通过Token移除客户端
        /// </summary>
        public bool RemoveClient(ulong token)
        {
            lock (_lock)
            {
                if (!_clientsByToken.TryGetValue(token, out Client client))
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

                return true;
            }
        }

        #endregion

        #region 客户端查询

        public bool TryGetClient(uint clientId, out Client client)
        {
            lock (_lock)
            {
                return _clientsById.TryGetValue(clientId, out client);
            }
        }

        public bool TryGetClient(ulong token, out Client client)
        {
            lock (_lock)
            {
                return _clientsByToken.TryGetValue(token, out client);
            }
        }

        public bool TryGetClientId(ulong token, out uint clientId)
        {
            lock (_lock)
            {
                if (_clientsByToken.TryGetValue(token, out Client client))
                {
                    clientId = client.Id;
                    return true;
                }

                clientId = 0;
                return false;
            }
        }

        public bool TryGetToken(uint clientId, out ulong token)
        {
            lock (_lock)
            {
                if (_clientsById.TryGetValue(clientId, out Client client))
                {
                    token = client.Token;
                    return true;
                }

                token = 0;
                return false;
            }
        }

        public bool TryGetClientId(TransportType transportType, uint transportSessionId, out uint clientId)
        {
            lock (_lock)
            {
                Dictionary<uint, uint> map = transportType == TransportType.TCP
                    ? _clientIdsByReliableSessionId
                    : _clientIdsByFastSessionId;

                return map.TryGetValue(transportSessionId, out clientId);
            }
        }

        public bool TryGetReliableSessionId(uint clientId, out uint reliableSessionId)
        {
            lock (_lock)
            {
                if (_clientsById.TryGetValue(clientId, out Client client) && client.ReliableSessionId != 0)
                {
                    reliableSessionId = client.ReliableSessionId;
                    return true;
                }

                reliableSessionId = 0;
                return false;
            }
        }

        public bool TryGetFastSessionId(uint clientId, out uint fastSessionId)
        {
            lock (_lock)
            {
                if (_clientsById.TryGetValue(clientId, out Client client) && client.FastSessionId != 0)
                {
                    fastSessionId = client.FastSessionId;
                    return true;
                }

                fastSessionId = 0;
                return false;
            }
        }

        public IEnumerable<Client> GetAllClients()
        {
            lock (_lock)
            {
                Client[] snapshot = new Client[_clientsById.Count];
                _clientsById.Values.CopyTo(snapshot, 0);
                return snapshot;
            }
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

            lock (_lock)
            {
                List<uint> toRemove = null;
                foreach (var kv in _clientsById)
                {
                    Client client = kv.Value;
                    if (client.DisconnectTime < 0f) continue;
                    if (currentTime - client.DisconnectTime >= gracePeriodSeconds)
                    {
                        toRemove ??= new List<uint>();
                        toRemove.Add(client.Id);
                    }
                }

                if (toRemove != null)
                {
                    foreach (uint id in toRemove)
                    {
                        RemoveClient(id);
                    }
                }
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _clientsById.Clear();
                _clientsByToken.Clear();
                _clientIdsByReliableSessionId.Clear();
                _clientIdsByFastSessionId.Clear();
                _nextClientId = 1;
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
