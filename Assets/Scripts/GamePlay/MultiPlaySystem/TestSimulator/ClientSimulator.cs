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
    /// 客户端同步测试夹具，只负责加入流程与测试实体生成。
    /// </summary>
    public sealed class ClientSimulator : IDisposable
    {
        private const string PlayerPrefabPath = "NetPlayer";

        private readonly Dictionary<uint, GameObject> _players = new();
        private readonly NetClient _netClient;
        private CharacterReplicationSystem _replicationSystem;
        private GameObject _playerPrefab;
        private bool _joinRequested;

        #region 属性
        public bool IsRunning { get; private set; }
        public uint ClientId => _netClient.ClientId;
        public int PlayerCount => _players.Count;
        #endregion

        public ClientSimulator(NetClient netClient)
        {
            _netClient = netClient ?? throw new ArgumentNullException(nameof(netClient));
        }

        public void Start()
        {
            if (IsRunning) return;
            _playerPrefab = Resources.Load<GameObject>(PlayerPrefabPath);
            if (_playerPrefab == null) throw new InvalidOperationException($"缺少 Resources Prefab：{PlayerPrefabPath}");

            _replicationSystem = Global.Get<CharacterReplicationSystem>();
            _replicationSystem?.SetClientEntityFactory(SpawnPlayer);
            _netClient.RegNetHandler<global::NetSync.Game_Join_Response>(NetEvent.GAME_JOIN_RESPONSE, HandleJoinResponse);
            IsRunning = true;
        }

        public void Update(float deltaTime)
        {
            if (!IsRunning || _joinRequested || ClientId == 0) return;
            _joinRequested = true;
            _netClient.SendReliable(NetEvent.GAME_JOIN_REQUEST,
                new global::NetSync.Game_Join_Request { ClientId = ClientId });
        }

        public void Dispose()
        {
            if (!IsRunning) return;
            _netClient.UnRegNetHandler(NetEvent.GAME_JOIN_RESPONSE);
            _replicationSystem?.SetClientEntityFactory(null);
            foreach (GameObject player in _players.Values)
            {
                if (player != null) UnityEngine.Object.Destroy(player);
            }
            _players.Clear();
            IsRunning = false;
        }

        private void HandleJoinResponse(global::NetSync.Game_Join_Response response)
        {
            if (response == null || !response.Accepted) _joinRequested = false;
        }

        private NetworkObjectIdentity SpawnPlayer(uint entityId, uint ownerClientId, bool isOwned)
        {
            if (_players.TryGetValue(entityId, out GameObject existing))
                return existing.GetComponent<NetworkObjectIdentity>();

            GameObject instance = UnityEngine.Object.Instantiate(_playerPrefab, Vector3.up, Quaternion.identity);
            instance.name = isOwned ? $"LocalPlayer_{entityId}" : $"RemotePlayer_{entityId}";
            NetworkObjectIdentity identity = instance.GetComponent<NetworkObjectIdentity>();
            if (identity == null)
            {
                throw new InvalidOperationException("NetPlayer Prefab 缺少 NetworkObjectIdentity。");
            }
            identity.Init(entityId, isOwned ? EntitySimulationMode.Predict : EntitySimulationMode.Replica, ownerClientId);
            _players.Add(entityId, instance);
            return identity;
        }
    }
}
