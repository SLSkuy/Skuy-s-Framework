using System;
using System.Collections.Generic;
using System.Net;
using System.Security.Cryptography;

namespace Network
{
    /// <summary>
    /// 客户端抽象管理，控制客户端在服务端的连接抽象
    /// </summary>
    public sealed class ServerClientRegister
    {
        /// <summary>
        /// KCP绑定信息
        /// </summary>
        public readonly struct KcpBindingInfo
        {
            public readonly uint ClientId;
            public readonly ulong Token;

            public KcpBindingInfo(uint clientId, ulong token)
            {
                ClientId = clientId;
                Token = token;
            }
        }

        /// <summary>
        /// 客户端连接快照
        /// </summary>
        public readonly struct ClientSnapshot
        {
            public readonly uint ClientId;
            public readonly EndPoint TcpEndPoint;
            public readonly EndPoint KcpEndPoint;
            public readonly bool TcpConnected;
            public readonly bool KcpConnected;

            public ClientSnapshot(uint clientId, EndPoint tcpEndPoint, EndPoint kcpEndPoint, bool tcpConnected, bool kcpConnected)
            {
                ClientId = clientId;
                TcpEndPoint = tcpEndPoint;
                KcpEndPoint = kcpEndPoint;
                TcpConnected = tcpConnected;
                KcpConnected = kcpConnected;
            }
        }

        /// <summary>
        /// 客户端连接抽象
        /// </summary>
        private sealed class ClientEntry
        {
            public uint ClientId;
            public ulong KcpToken;
            public EndPoint TcpEndPoint;
            public EndPoint KcpEndPoint;
            public bool TcpConnected;
            public bool KcpConnected;
        }

        private readonly object _lock = new object();
        private readonly Dictionary<uint, ClientEntry> _clientsById = new Dictionary<uint, ClientEntry>();
        private readonly Dictionary<ulong, ClientEntry> _clientsByToken = new Dictionary<ulong, ClientEntry>();
        private uint _nextClientId = 1;

        public KcpBindingInfo RegisterTcpClient(EndPoint remoteEndPoint)
        {
            lock (_lock)
            {
                ClientEntry entry = new ClientEntry
                {
                    ClientId = _nextClientId++,
                    KcpToken = CreateUniqueToken(),
                    TcpEndPoint = remoteEndPoint,
                    TcpConnected = true
                };

                _clientsById.Add(entry.ClientId, entry);
                _clientsByToken.Add(entry.KcpToken, entry);
                return new KcpBindingInfo(entry.ClientId, entry.KcpToken);
            }
        }

        public bool TryGetKcpBinding(uint clientId, out KcpBindingInfo bindingInfo)
        {
            lock (_lock)
            {
                if (_clientsById.TryGetValue(clientId, out ClientEntry entry) && entry.TcpConnected)
                {
                    bindingInfo = new KcpBindingInfo(entry.ClientId, entry.KcpToken);
                    return true;
                }
            }

            bindingInfo = default;
            return false;
        }

        public bool TryGetClient(uint clientId, out ClientSnapshot client)
        {
            lock (_lock)
            {
                if (_clientsById.TryGetValue(clientId, out ClientEntry entry))
                {
                    client = new ClientSnapshot(entry.ClientId, entry.TcpEndPoint, entry.KcpEndPoint, entry.TcpConnected, entry.KcpConnected);
                    return true;
                }
            }

            client = default;
            return false;
        }

        public bool TryBindKcpClient(uint clientId, ulong token, EndPoint remoteEndPoint, out uint boundClientId)
        {
            lock (_lock)
            {
                if (!_clientsByToken.TryGetValue(token, out ClientEntry entry) || entry.ClientId != clientId || !entry.TcpConnected)
                {
                    boundClientId = 0;
                    return false;
                }

                entry.KcpEndPoint = remoteEndPoint;
                entry.KcpConnected = true;
                boundClientId = entry.ClientId;
                return true;
            }
        }

        public void ReleaseTcpClient(uint clientId)
        {
            lock (_lock)
            {
                if (!_clientsById.TryGetValue(clientId, out ClientEntry entry))
                {
                    return;
                }

                _clientsByToken.Remove(entry.KcpToken);
                _clientsById.Remove(clientId);
            }
        }

        public void ReleaseKcpClient(uint clientId)
        {
            lock (_lock)
            {
                if (_clientsById.TryGetValue(clientId, out ClientEntry entry))
                {
                    entry.KcpConnected = false;
                    entry.KcpEndPoint = null;
                }
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _clientsById.Clear();
                _clientsByToken.Clear();
                _nextClientId = 1;
            }
        }

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
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }

            return BitConverter.ToUInt64(bytes, 0);
        }
    }
}
