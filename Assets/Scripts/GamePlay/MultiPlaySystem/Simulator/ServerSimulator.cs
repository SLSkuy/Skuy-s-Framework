using System;
using System.Collections.Generic;
using Events;
using Network;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GamePlay.MultiPlaySystem
{
    /// <summary>
    /// 服务端同步测试夹具：监听加入并仅在客户端连接后生成其权威角色。
    /// </summary>
    public sealed class ServerSimulator : IDisposable
    {
        private readonly Dictionary<uint, GameObject> _players = new();
        private readonly NetServer _netServer;
        private GameObject _playerPrefab;

        #region 属性
        public bool IsRunning { get; private set; }
        public int PlayerCount => _players.Count;
        #endregion

        public ServerSimulator(NetServer netServer)
        {
            _netServer = netServer ?? throw new ArgumentNullException(nameof(netServer));
        }

        public void Start()
        {
            if (IsRunning) return;
            _playerPrefab = PlayerSpawner.LoadPrefab();
            _netServer.RegNetHandler<global::NetSync.Game_Join_Request>(NetEvent.GAME_JOIN_REQUEST, HandleJoinRequest);
            _netServer.OnClientRemoved += HandleClientRemoved;
            IsRunning = true;
        }

        public void Update(float deltaTime)
        {
        }

        public void Dispose()
        {
            if (!IsRunning) return;
            _netServer.UnRegNetHandler(NetEvent.GAME_JOIN_REQUEST);
            _netServer.OnClientRemoved -= HandleClientRemoved;
            foreach (GameObject player in _players.Values)
            {
                if (player != null) Object.Destroy(player);
            }
            _players.Clear();
            IsRunning = false;
        }

        private void HandleJoinRequest(uint clientId, global::NetSync.Game_Join_Request request)
        {
            bool accepted = request != null && request.ClientId == clientId;
            if (accepted && !_players.ContainsKey(clientId)) SpawnPlayer(clientId);
            _netServer.SendReliable(clientId, NetEvent.GAME_JOIN_RESPONSE,
                new global::NetSync.Game_Join_Response { Accepted = accepted });
        }

        private void SpawnPlayer(uint clientId)
        {
            NetworkObjectIdentity identity = PlayerSpawner.Spawn(
                _playerPrefab,
                new Vector3((clientId - 1u) * 2f, 1f, 0f),
                $"ServerPlayer_{clientId}",
                clientId,
                EntitySimulationMode.Authority,
                clientId);
            _players.Add(clientId, identity.gameObject);
        }

        private void HandleClientRemoved(uint clientId)
        {
            if (!_players.Remove(clientId, out GameObject player)) return;
            if (player != null) Object.Destroy(player);
        }
    }
}
