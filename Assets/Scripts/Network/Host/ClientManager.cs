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

            public Client(uint id, ulong token)
            {
                Id = id;
                Token = token;
                ReliableSessionId = 0;
                FastSessionId = 0;
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

        public bool BindFastSession(ulong token, uint fastSessionId)
        {
            lock (_lock)
            {
                if (!TryGetClient(token, out var client))
                {
                    return false;
                }

                if (client.FastSessionId != 0)
                {
                    _clientIdsByFastSessionId.Remove(client.FastSessionId);
                }

                client.FastSessionId = fastSessionId;
                _clientsById[client.Id] = client;
                _clientsByToken[token] = client;
                _clientIdsByFastSessionId[fastSessionId] = client.Id;
                return true;
            }
        }

        public bool UnBindReliableSession(uint reliableSessionId, out uint clientId)
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

                if (!CheckClientConnected(clientId))
                {
                    Debug.Log($"[ClientManager] Client {clientId} has not been connected, Removed");
                    RemoveClient(clientId);
                }

                return true;
            }
        }

        public bool UnbindFastSession(uint fastSessionId, out uint clientId)
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
                
                if (!CheckClientConnected(clientId))
                {
                    Debug.Log($"[ClientManager] Client {clientId} has not been connected, Removed");
                    RemoveClient(clientId);
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
