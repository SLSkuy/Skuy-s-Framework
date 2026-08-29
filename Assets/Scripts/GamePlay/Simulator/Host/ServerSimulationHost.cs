using System.Collections.Generic;
using Framework;
using GamePlay.EntitySystem;
using GamePlay.MultiPlaySystem;
using UnityEngine;
using Utils;

namespace GamePlay.Simulator
{
    /// <summary>
    /// 服务端会话：持核、Join 生成 Authority、缓冲意图、按配置广播快照。
    /// </summary>
    public sealed class ServerSimulationHost : SubSystemBase
    {
        private readonly MatchRoom _room = new();
        private readonly ServerHandler _handler = new();
        private readonly Dictionary<uint, GameObject> _pawns = new();
        private readonly Dictionary<uint, AuthorityInputProvider> _inputs = new();
        private readonly List<uint> _leaveBuffer = new();
        private Simulator _simulator;
        private GameObject _playerPrefab;
        private int _snapshotAccumulator;

        #region 属性
        public override int Priority => 500;
        public Simulator Simulator => _simulator;
        public bool IsSessionRunning { get; private set; }
        #endregion

        public bool StartSession()
        {
            if (IsSessionRunning) return true;
            if (IsLocalSessionRunning()) return false;

            _playerPrefab = PlayerSpawner.LoadPrefab();
            _handler.OnJoinRequest += HandleJoinRequest;
            _handler.OnPlayerInput += HandlePlayerInput;
            if (!_handler.BindMessage())
            {
                _handler.OnJoinRequest -= HandleJoinRequest;
                _handler.OnPlayerInput -= HandlePlayerInput;
                return false;
            }
            _simulator.Tick += HandleSimulatorTick;
            _simulator.StartClock();
            IsSessionRunning = true;
            return true;
        }

        public void StopSession()
        {
            if (!IsSessionRunning) return;

            _simulator.Tick -= HandleSimulatorTick;
            _handler.UnBindMessage();
            _handler.OnJoinRequest -= HandleJoinRequest;
            _handler.OnPlayerInput -= HandlePlayerInput;

            _room.CopyClientIds(_leaveBuffer);
            for (int i = 0; i < _leaveBuffer.Count; i++)
            {
                LeaveClient(_leaveBuffer[i]);
            }

            _room.Clear();
            _simulator.StopClock();
            _snapshotAccumulator = 0;
            IsSessionRunning = false;
        }

        #region 子系统生命周期

        public override void Init()
        {
            _simulator = new Simulator();
            _simulator.Init();
        }

        public override void Update(float deltaTime)
        {
            _simulator.Update(deltaTime);
        }

        public override void Destroy()
        {
            StopSession();
            _simulator.Destroy();
            _simulator = null;
        }

        #endregion

        private void HandleJoinRequest(uint clientId, global::NetSync.Game_Join_Request request)
        {
            bool accepted = request != null && request.ClientId == clientId && clientId != 0;
            if (accepted && _room.TryJoin(clientId, out uint entityId))
            {
                SpawnAuthority(clientId, entityId);
            }

            _handler.SendJoinResponse(clientId, accepted);
        }

        private void HandlePlayerInput(uint clientId, global::NetSync.Player_Input input)
        {
            if (input == null || !_room.TryAuthorize(clientId, input.EntityId)) return;
            if (!_inputs.TryGetValue(input.EntityId, out AuthorityInputProvider source)) return;
            source.Enqueue(input.InputTick, ProtoUtils.ToInputState(input));
        }

        private void LeaveClient(uint clientId)
        {
            if (!_room.TryLeave(clientId, out uint entityId)) return;

            _simulator.SetInputSource(entityId, null);
            _simulator.Unregister(entityId);
            _inputs.Remove(entityId);
            if (_pawns.Remove(entityId, out GameObject pawn) && pawn != null)
            {
                Object.Destroy(pawn);
            }
        }

        private void SpawnAuthority(uint clientId, uint entityId)
        {
            if (_pawns.ContainsKey(entityId)) return;

            SimulationConfig config = SimulationConfig.Instance;
            Vector3 position = new((clientId - 1u) * 2f, 1f, 0f);
            EntityObjectIdentity identity = PlayerSpawner.Spawn(
                _playerPrefab, position, $"ServerPlayer_{entityId}", entityId, EntityObjectRole.Authority, clientId);
            EntityCharacter character = identity.GetComponent<EntityCharacter>();
            _simulator.Register(identity, character);

            AuthorityInputProvider provider = new(config.maxBufferedInputs, config.maxFutureInputTicks);
            _inputs[entityId] = provider;
            _simulator.SetInputSource(entityId, provider);
            _pawns[entityId] = identity.gameObject;
        }

        private void HandleSimulatorTick(uint tick, float deltaTime)
        {
            SimulationConfig config = SimulationConfig.Instance;
            _snapshotAccumulator += config.snapshotTickRate;
            if (_snapshotAccumulator < config.simulationTickRate) return;
            _snapshotAccumulator -= config.simulationTickRate;
            BroadcastSnapshot(tick);
        }

        private void BroadcastSnapshot(uint tick)
        {
            global::NetSync.World_Snapshot world = new() { SnapshotTick = tick };
            _simulator.ForEachRegistered((identity, _) =>
            {
                if (identity == null || !identity.IsAuthority) return;
                if (!_simulator.TryCapture(identity.EntityId, out EntityRollbackState state)) return;
                uint lastProcessed = _inputs.TryGetValue(identity.EntityId, out AuthorityInputProvider source)
                    ? source.LastProcessedTick : 0;
                world.CharacterSnapshots.Add(ProtoUtils.ToCharacterSnapshotMessage(
                    state, identity.EntityId, identity.OwnerClientId, tick, lastProcessed));
            });
            _handler.BroadcastWorldSnapshot(world);
        }

        private static bool IsLocalSessionRunning()
        {
            LocalSimulationHost localHost = Global.Get<LocalSimulationHost>();
            return localHost != null && localHost.IsSessionRunning;
        }
    }
}
