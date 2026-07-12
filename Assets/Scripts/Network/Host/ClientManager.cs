using System;
using System.Collections.Generic;

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
            public readonly uint ClientId;
            public readonly ulong Token;
            public uint ReliableSessionId;
            public uint FastSessionId;

            public Client(uint clientId, ulong token)
            {
                ClientId = clientId;
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

        #region 客户端管理

        /// <summary>
        /// 注册新客户端连接，生成唯一Token
        /// </summary>
        public Client AddClient()
        {
            lock (_lock)
            {
                ulong token = CreateUniqueToken();
                Client client = new Client(_nextClientId, token);

                _clientsById[_nextClientId] = client;
                _clientsByToken[token] = client;

                _nextClientId++;
                return client;
            }
        }

        /// <summary>
        /// 移除客户端
        /// </summary>
        public bool RemoveClient(uint clientId)
        {
            lock (_lock)
            {
                if (!_clientsById.TryGetValue(clientId, out Client client))
                {
                    return false;
                }

                _clientsById.Remove(clientId);
                _clientsByToken.Remove(client.Token);
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

                _clientsById.Remove(client.ClientId);
                _clientsByToken.Remove(token);
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
                    clientId = client.ClientId;
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
