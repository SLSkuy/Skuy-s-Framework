using System;
using System.Collections.Generic;
using Events;
using Framework;
using GamePlay.EntitySystem;
using Network;
using UnityEngine;
using Utils;

namespace GamePlay.NetSync
{
    public sealed class ClientSimulator : IDisposable
    {
        private sealed class RemoteEntry
        {
            public GameObject GameObject;
            public EntityCharacter Character;
            public NetEntityIdentity Identity;
            public RemoteInterpDriver Driver;
        }

        private const string PlayerPrefabPath = "Prefabs/NetPlayerCharacter";

        private readonly NetClient _netClient;
        private readonly TickSystem _simulationTicks;
        private readonly Dictionary<uint, RemoteEntry> _remotePlayers = new();
        private readonly HashSet<uint> _snapshotEntities = new();
        private readonly List<uint> _staleEntities = new();

        private GameObject _playerPrefab;
        private GameObject _localPlayerObject;
        private EntityCharacter _localCharacter;
        private NetEntityIdentity _localIdentity;
        private ClientPredictDriver _localController;
        private LocalInputProvider _localInput;
        private uint _inputTick;
        private bool _joinRequested;
        private bool _started;

        public uint CurrentInputTick => _inputTick;
        public uint CurrentSimulationTick => _simulationTicks.CurrentTick;
        public uint ClientId => _netClient.ClientId;
        public bool IsJoined => _localPlayerObject != null;
        public int EntityCount => _remotePlayers.Count + (IsJoined ? 1 : 0);

        public ClientSimulator(NetClient netClient)
        {
            _netClient = netClient ?? throw new ArgumentNullException(nameof(netClient));
            SyncConfig config = SyncConfig.Instance;
            _simulationTicks = new TickSystem(config.simulationTickRate);
            _simulationTicks.OnTick += SendInput;
        }

        public void Start()
        {
            if (_started) return;

            _playerPrefab = Resources.Load<GameObject>(PlayerPrefabPath);
            if (_playerPrefab == null)
            {
                throw new InvalidOperationException($"Missing Resources prefab: {PlayerPrefabPath}");
            }

            _netClient.RegNetHandler<global::NetSync.Game_Join_Response>(
                NetEvent.GAME_JOIN_RESPONSE,
                HandleJoinResponse);
            _netClient.RegNetHandler<global::NetSync.World_Snapshot>(
                NetEvent.WORLD_SNAPSHOT,
                HandleWorldSnapshot);
            _netClient.RegNetHandler<global::NetSync.Player_Snapshot>(
                NetEvent.PLAYER_SNAPSHOT,
                HandlePlayerSnapshot);
            _simulationTicks.Start();
            _started = true;
        }

        public void Update(float deltaTime)
        {
            if (!_started) return;

            TryJoinGame();
            _simulationTicks.Update(deltaTime);

            // 每帧推进所有远端插值（替代旧 RemotePlayerController.Update 自驱）
            foreach (RemoteEntry remote in _remotePlayers.Values)
            {
                remote.Driver.UpdateInterpolation(deltaTime);
            }
        }

        private void TryJoinGame()
        {
            if (_joinRequested || _netClient.ClientId == 0) return;

            _joinRequested = true;
            _netClient.SendReliable(
                NetEvent.GAME_JOIN_REQUEST,
                new global::NetSync.Game_Join_Request { ClientId = _netClient.ClientId });
        }

        private void HandleJoinResponse(global::NetSync.Game_Join_Response response)
        {
            if (response == null || !response.Accepted || IsJoined) return;
            SpawnLocalPlayer(_netClient.ClientId);
        }

        private void SendInput(uint commandTick)
        {
            if (!IsJoined || !_netClient.HasFastChannel) return;

            _inputTick++;
            InputState input = _localController.OnInputTick(_inputTick, _simulationTicks.TickDeltaTime);
            _netClient.Send(
                NetEvent.PLAYER_INPUT,
                NetSyncUtils.ToPlayerInput(_localIdentity.EntityId, _inputTick, input));
        }

        private void HandleWorldSnapshot(global::NetSync.World_Snapshot world)
        {
            if (world == null) return;

            _snapshotEntities.Clear();
            foreach (global::NetSync.Player_Snapshot snapshot in world.PlayerSnapshots)
            {
                _snapshotEntities.Add(snapshot.EntityId);
                ConsumeSnapshot(snapshot);
            }

            _staleEntities.Clear();
            foreach (uint entityId in _remotePlayers.Keys)
            {
                if (!_snapshotEntities.Contains(entityId)) _staleEntities.Add(entityId);
            }

            foreach (uint entityId in _staleEntities)
            {
                RemoveRemotePlayer(entityId);
            }
        }

        private void HandlePlayerSnapshot(global::NetSync.Player_Snapshot snapshot)
        {
            if (snapshot != null) ConsumeSnapshot(snapshot);
        }

        private void ConsumeSnapshot(global::NetSync.Player_Snapshot message)
        {
            NetPlayerSnapshot snapshot = NetSyncUtils.ToPlayerSnapshot(message);
            if (_localIdentity != null && snapshot.EntityId == _localIdentity.EntityId)
            {
                _localController.OnAuthoritySnapshot(snapshot, _simulationTicks.TickDeltaTime);
                return;
            }

            if (!_remotePlayers.TryGetValue(snapshot.EntityId, out RemoteEntry remote))
            {
                remote = SpawnRemotePlayer(snapshot.EntityId, snapshot.Position);
            }

            remote.Driver.AddSnapshot(snapshot);
        }

        private void SpawnLocalPlayer(uint entityId)
        {
            _localPlayerObject = UnityEngine.Object.Instantiate(
                _playerPrefab,
                Vector3.up,
                Quaternion.identity);
            _localPlayerObject.name = $"LocalPlayer_{entityId}";

            _localIdentity = _localPlayerObject.GetComponent<NetEntityIdentity>();
            _localIdentity.Init(entityId, NetEntityRole.Predict);

            _localCharacter = _localPlayerObject.GetComponent<EntityCharacter>();
            _localInput = _localPlayerObject.AddComponent<LocalInputProvider>();
            _localController = new ClientPredictDriver();

            NetworkVisualSmoother visualSmoother = _localPlayerObject.GetComponent<NetworkVisualSmoother>();
            visualSmoother?.BeginFollowing();

            _localController.Configure(_localCharacter, _localIdentity, _localInput, visualSmoother);
        }

        private RemoteEntry SpawnRemotePlayer(uint entityId, Vector3 position)
        {
            GameObject instance = UnityEngine.Object.Instantiate(
                _playerPrefab,
                position,
                Quaternion.identity);
            instance.name = $"RemotePlayer_{entityId}";

            NetEntityIdentity identity = instance.GetComponent<NetEntityIdentity>();
            identity.Init(entityId, NetEntityRole.Replica);

            EntityCharacter character = instance.GetComponent<EntityCharacter>();
            RemoteInterpDriver driver = new RemoteInterpDriver();
            driver.Configure(character, identity);

            RemoteEntry entry = new()
            {
                GameObject = instance,
                Character = character,
                Identity = identity,
                Driver = driver
            };
            _remotePlayers.Add(entityId, entry);
            return entry;
        }

        private void RemoveRemotePlayer(uint entityId)
        {
            if (!_remotePlayers.Remove(entityId, out RemoteEntry remote)) return;
            if (remote.GameObject != null) UnityEngine.Object.Destroy(remote.GameObject);
        }

        public void Dispose()
        {
            if (!_started) return;

            _simulationTicks.Stop();
            _simulationTicks.OnTick -= SendInput;
            _netClient.UnRegNetHandler(NetEvent.GAME_JOIN_RESPONSE);
            _netClient.UnRegNetHandler(NetEvent.WORLD_SNAPSHOT);
            _netClient.UnRegNetHandler(NetEvent.PLAYER_SNAPSHOT);

            if (_localPlayerObject != null) UnityEngine.Object.Destroy(_localPlayerObject);
            foreach (RemoteEntry remote in _remotePlayers.Values)
            {
                if (remote.GameObject != null) UnityEngine.Object.Destroy(remote.GameObject);
            }

            _remotePlayers.Clear();
            _localPlayerObject = null;
            _localCharacter = null;
            _localIdentity = null;
            _localController = null;
            _localInput = null;
            _started = false;
        }
    }
}
