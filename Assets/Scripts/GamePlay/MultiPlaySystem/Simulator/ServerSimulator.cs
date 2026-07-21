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
    public sealed class ServerSimulator : IDisposable
    {
        private sealed class PlayerEntry
        {
            public GameObject GameObject;
            public NetPlayerCharacter Character;
            public ServerPlayerController Controller;
        }

        private const string PlayerPrefabPath = "Prefabs/NetPlayerCharacter";

        private readonly NetServer _netServer;
        private readonly TickSystem _simulationTicks;
        private readonly Dictionary<uint, PlayerEntry> _players = new();
        private readonly int _simulationTickRate;
        private readonly int _snapshotTickRate;

        private GameObject _playerPrefab;
        private int _snapshotAccumulator;
        private bool _started;

        public uint CurrentTick => _simulationTicks.CurrentTick;
        public int PlayerCount => _players.Count;

        public ServerSimulator(NetServer netServer)
        {
            _netServer = netServer ?? throw new ArgumentNullException(nameof(netServer));
            SyncConfig config = SyncConfig.Instance;
            _simulationTickRate = Mathf.Max(1, config.simulationTickRate);
            _snapshotTickRate = Mathf.Max(1, config.snapshotTickRate);
            _simulationTicks = new TickSystem(_simulationTickRate);
            _simulationTicks.OnTick += SimulateTick;
        }

        public void Start()
        {
            if (_started) return;

            _playerPrefab = Resources.Load<GameObject>(PlayerPrefabPath);
            if (_playerPrefab == null)
            {
                throw new InvalidOperationException($"Missing Resources prefab: {PlayerPrefabPath}");
            }

            _netServer.RegNetHandler<global::NetSync.Game_Join_Request>(
                NetEvent.GAME_JOIN_REQUEST,
                HandleJoinRequest);
            _netServer.RegNetHandler<global::NetSync.Player_Input>(
                NetEvent.PLAYER_INPUT,
                HandlePlayerInput);
            _netServer.OnClientRemoved += HandleClientRemoved;
            _simulationTicks.Start();
            _started = true;
        }

        public void Update(float deltaTime)
        {
            if (_started) _simulationTicks.Update(deltaTime);
        }

        private void HandleJoinRequest(uint clientId, global::NetSync.Game_Join_Request request)
        {
            bool accepted = request != null && request.ClientId == clientId;
            if (accepted && !_players.ContainsKey(clientId))
            {
                SpawnPlayer(clientId);
            }

            _netServer.SendReliable(
                clientId,
                NetEvent.GAME_JOIN_RESPONSE,
                new global::NetSync.Game_Join_Response { Accepted = accepted });
        }

        private void HandlePlayerInput(uint clientId, global::NetSync.Player_Input input)
        {
            if (input == null || input.EntityId != clientId) return;
            if (!_players.TryGetValue(clientId, out PlayerEntry player)) return;

            player.Controller.ReceiveInput(input.InputTick, NetSyncUtils.ToInputState(input));
        }

        private void SimulateTick(uint simulationTick)
        {
            foreach (PlayerEntry player in _players.Values)
            {
                player.Controller.Simulate(_simulationTicks.TickDeltaTime);
            }

            _snapshotAccumulator += _snapshotTickRate;
            if (_snapshotAccumulator < _simulationTickRate) return;

            _snapshotAccumulator -= _simulationTickRate;
            BroadcastSnapshot(simulationTick);
        }

        private void BroadcastSnapshot(uint simulationTick)
        {
            if (!_netServer.HasFastChannel || _players.Count == 0) return;

            global::NetSync.World_Snapshot world = new()
            {
                SnapshotTick = simulationTick
            };

            foreach (PlayerEntry player in _players.Values)
            {
                NetPlayerSnapshot snapshot = player.Controller.CaptureSnapshot(simulationTick);
                world.PlayerSnapshots.Add(NetSyncUtils.ToPlayerSnapshot(snapshot));
            }

            _netServer.Broadcast(NetEvent.WORLD_SNAPSHOT, world);
        }

        private void SpawnPlayer(uint clientId)
        {
            Vector3 position = new((clientId - 1u) * 2f, 1f, 0f);
            GameObject instance = UnityEngine.Object.Instantiate(
                _playerPrefab,
                position,
                Quaternion.identity);
            instance.name = $"ServerPlayer_{clientId}";

            NetPlayerCharacter character = instance.GetComponent<NetPlayerCharacter>();
            character.Init(clientId, NetEntityRole.Authority);

            NetInputProvider inputProvider = instance.AddComponent<NetInputProvider>();
            ServerPlayerController controller = instance.AddComponent<ServerPlayerController>();
            controller.Configure(character, inputProvider);

            _players.Add(clientId, new PlayerEntry
            {
                GameObject = instance,
                Character = character,
                Controller = controller
            });
        }

        private void HandleClientRemoved(uint clientId)
        {
            if (!_players.Remove(clientId, out PlayerEntry player)) return;
            if (player.GameObject != null) UnityEngine.Object.Destroy(player.GameObject);
        }

        public void Dispose()
        {
            if (!_started) return;

            _simulationTicks.Stop();
            _simulationTicks.OnTick -= SimulateTick;
            _netServer.UnRegNetHandler(NetEvent.GAME_JOIN_REQUEST);
            _netServer.UnRegNetHandler(NetEvent.PLAYER_INPUT);
            _netServer.OnClientRemoved -= HandleClientRemoved;

            foreach (PlayerEntry player in _players.Values)
            {
                if (player.GameObject != null) UnityEngine.Object.Destroy(player.GameObject);
            }

            _players.Clear();
            _started = false;
        }
    }
}
