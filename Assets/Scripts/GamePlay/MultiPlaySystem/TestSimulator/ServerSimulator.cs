using System;
using System.Collections.Generic;
using Events;
using Framework;
using GamePlay.EntitySystem;
using Network;
using UnityEngine;

namespace GamePlay.MultiPlaySystem
{
    /// <summary>
    /// 服务端同步测试夹具，只负责连接、生成和销毁测试实体。
    /// </summary>
    public sealed class ServerSimulator : IDisposable
    {
        private const string PlayerPrefabPath = "NetPlayer";

        private const uint ServerDrivenEntityId = 10001;

        private readonly Dictionary<uint, GameObject> _players = new();
        private readonly NetServer _netServer;
        private GameObject _playerPrefab;
        private GameObject _serverDrivenCharacter;

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
            _playerPrefab = Resources.Load<GameObject>(PlayerPrefabPath);
            if (_playerPrefab == null) throw new InvalidOperationException($"缺少 Resources Prefab：{PlayerPrefabPath}");

            _netServer.RegNetHandler<global::NetSync.Game_Join_Request>(NetEvent.GAME_JOIN_REQUEST, HandleJoinRequest);
            _netServer.OnClientRemoved += HandleClientRemoved;
            SpawnServerDrivenCharacter();
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
                if (player != null) UnityEngine.Object.Destroy(player);
            }
            _players.Clear();
            if (_serverDrivenCharacter != null) UnityEngine.Object.Destroy(_serverDrivenCharacter);
            _serverDrivenCharacter = null;
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
            GameObject instance = UnityEngine.Object.Instantiate(
                _playerPrefab,
                new Vector3((clientId - 1u) * 2f, 1f, 0f),
                Quaternion.identity);
            instance.name = $"ServerPlayer_{clientId}";
            NetworkObjectIdentity identity = instance.GetComponent<NetworkObjectIdentity>();
            if (identity == null)
            {
                throw new InvalidOperationException("NetPlayer Prefab 缺少 NetworkObjectIdentity。");
            }
            identity.Init(clientId, EntitySimulationMode.Authority, clientId);
            _players.Add(clientId, instance);
        }

        private void SpawnServerDrivenCharacter()
        {
            GameObject instance = UnityEngine.Object.Instantiate(
                _playerPrefab,
                new Vector3(0f, 1f, 4f),
                Quaternion.identity);
            instance.name = "ServerDrivenCharacter";
            NetworkObjectIdentity identity = instance.GetComponent<NetworkObjectIdentity>();
            if (identity == null)
            {
                throw new InvalidOperationException("NetPlayer Prefab 缺少 NetworkObjectIdentity。");
            }

            identity.Init(ServerDrivenEntityId, EntitySimulationMode.Authority, 0);
            _serverDrivenCharacter = instance;
        }

        private void HandleClientRemoved(uint clientId)
        {
            if (!_players.Remove(clientId, out GameObject player)) return;
            if (player != null) UnityEngine.Object.Destroy(player);
        }
    }
}
