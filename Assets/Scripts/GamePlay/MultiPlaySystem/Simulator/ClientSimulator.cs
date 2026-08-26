using System;
using System.Collections.Generic;
using Events;
using Framework;
using GamePlay.Simulator;
using Network;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GamePlay.MultiPlaySystem
{
    /// <summary>
    /// 客户端同步测试夹具，只负责加入流程与测试实体生成。
    /// </summary>
    public sealed class ClientSimulator : IDisposable
    {
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
            _playerPrefab = PlayerSpawner.LoadPrefab();
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
                if (player != null) Object.Destroy(player);
            }
            _players.Clear();
            IsRunning = false;
        }

        private void HandleJoinResponse(global::NetSync.Game_Join_Response response)
        {
            if (response == null || !response.Accepted) _joinRequested = false;
        }

        private EntityObjectIdentity SpawnPlayer(uint entityId, uint ownerClientId, bool isOwned)
        {
            if (_players.TryGetValue(entityId, out GameObject existing))
                return existing.GetComponent<EntityObjectIdentity>();

            string objectName = isOwned ? $"LocalPlayer_{entityId}" : $"RemotePlayer_{entityId}";
            EntityObjectRole role = isOwned ? EntityObjectRole.Predict : EntityObjectRole.Replica;
            EntityObjectIdentity identity = PlayerSpawner.Spawn(
                _playerPrefab, Vector3.up, objectName, entityId, role, ownerClientId);
            _players.Add(entityId, identity.gameObject);
            return identity;
        }
    }
}
