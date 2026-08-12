using System;
using System.Collections.Generic;
using Core;
using Events;
using Framework;
using GamePlay.EntitySystem;
using Network;
using UnityEngine;
using Utils;

namespace GamePlay.NetSync
{
    /// <summary>
    /// 网络实体复制系统：统一处理输入、权威模拟和 Transform 快照。
    /// </summary>
    public sealed class EntityReplicationSystem : SubSystemBase
    {
        private sealed class EntityEntry
        {
            public BaseEntity Entity;
            public NetworkObjectIdentity Identity;
            public EntityCommandQueue<InputState> Commands;
            public EntityInputCommandBuilder CommandBuilder;
            public uint OwnerClientId;
            public uint LastAppliedSnapshotTick;
            public uint NextInputTick;
        }

        private readonly Dictionary<uint, EntityEntry> _entities = new();
        private NetworkTimeSystem _networkTime;
        private NetClient _client;
        private NetServer _server;
        private int _snapshotAccumulator;
        private bool _clientHandlerBound;
        private bool _serverHandlerBound;
        private Func<uint, uint, bool, BaseEntity> _clientEntityFactory;

        #region 属性

        public override SubSystemPriority Priority => SubSystemPriority.NetSyncManager;
        public uint CurrentTick => _networkTime?.CurrentTick ?? 0;
        public int RegisteredEntityCount => _entities.Count;

        #endregion

        public override void Init()
        {
            _networkTime = Global.Get<NetworkTimeSystem>();
            if (_networkTime != null) _networkTime.Tick += SimulateTick;
            BindNetworkHandlers();
        }

        /// <summary>
        /// 注册网络实体及其所有者。
        /// </summary>
        public bool Register(BaseEntity entity, NetworkObjectIdentity identity, uint ownerClientId = 0)
        {
            if (entity == null || identity == null || identity.EntityId == 0) return false;
            if (_entities.ContainsKey(identity.EntityId)) return false;

            int historyCapacity = Mathf.Max(2, SyncConfig.Instance.maxBufferedInputs);
            _entities.Add(identity.EntityId, new EntityEntry
            {
                Entity = entity,
                Identity = identity,
                OwnerClientId = ownerClientId,
                Commands = new EntityCommandQueue<InputState>(historyCapacity),
                CommandBuilder = new EntityInputCommandBuilder()
            });
            return true;
        }

        /// <summary>
        /// 注销网络实体并清理其命令与预测历史。
        /// </summary>
        public void Unregister(uint entityId)
        {
            _entities.Remove(entityId);
        }

        /// <summary>
        /// 配置客户端实体工厂，参数依次为对象 ID、Owner ID 和是否本地拥有。
        /// </summary>
        public void SetClientEntityFactory(Func<uint, uint, bool, BaseEntity> entityFactory)
        {
            _clientEntityFactory = entityFactory;
        }

        /// <summary>
        /// 接收并校验一个客户端输入。该入口供传输层与集成测试共同使用。
        /// </summary>
        public bool TryAcceptInput(uint clientId, global::NetSync.Player_Input input, uint serverTick)
        {
            if (input == null || !_entities.TryGetValue(input.EntityId, out EntityEntry entry)) return false;
            if (entry.Identity.Role != EntitySimulationMode.Authority || entry.OwnerClientId != clientId) return false;

            SyncConfig config = SyncConfig.Instance;
            InputState state = NetSyncUtils.ToInputState(input);
            if (!IsFinite(state.MoveInput) || !IsFinite(state.AimInput)) return false;
            float maxMagnitudeSquared = config.maxInputVectorMagnitude * config.maxInputVectorMagnitude;
            if (state.MoveInput.sqrMagnitude > maxMagnitudeSquared || state.AimInput.sqrMagnitude > maxMagnitudeSquared)
                return false;

            return entry.Commands.Enqueue(
                input.InputTick,
                serverTick,
                (uint)config.maxPastInputTicks,
                (uint)config.maxFutureInputTicks,
                state);
        }

        /// <summary>
        /// 推进全部权威实体一个固定 Tick；客户端实体只由收到的快照驱动。
        /// </summary>
        public void AdvanceSimulation(uint tick, float deltaTime)
        {
            foreach (EntityEntry entry in _entities.Values)
            {
                switch (entry.Identity.Role)
                {
                    case EntitySimulationMode.Authority:
                        SimulateAuthorityTick(entry, tick, deltaTime);
                        break;
                    case EntitySimulationMode.Predict:
                        SendOwnedInput(entry, tick);
                        break;
                }
            }
        }

        /// <summary>
        /// 捕获当前权威世界快照。Owner 元数据与 Transform 状态在同一对象记录中下发。
        /// </summary>
        public global::NetSync.World_Snapshot CreateWorldSnapshot(uint tick)
        {
            global::NetSync.World_Snapshot world = new() { SnapshotTick = tick };
            foreach (EntityEntry entry in _entities.Values)
            {
                if (entry.Identity.Role != EntitySimulationMode.Authority) continue;
                EntitySimulationState state = entry.Entity.CaptureSimulationState();
                if (!state.IsFinite()) continue;
                world.TransformSnapshots.Add(NetSyncUtils.ToTransformSnapshotMessage(
                    state,
                    entry.Identity.NetworkObjectId,
                    entry.OwnerClientId,
                    tick,
                    entry.Commands.LastProcessedTick));
            }

            return world;
        }

        /// <summary>
        /// 应用权威世界快照，并依据 Owner 元数据选择 Predict 或 Replica。
        /// </summary>
        public void ApplyWorldSnapshot(global::NetSync.World_Snapshot world, uint localClientId)
        {
            if (world == null) return;
            foreach (global::NetSync.Transform_Snapshot snapshot in world.TransformSnapshots)
            {
                bool isOwned = snapshot.OwnerClientId != 0 && snapshot.OwnerClientId == localClientId;
                EntitySimulationMode expectedRole = isOwned ? EntitySimulationMode.Predict : EntitySimulationMode.Replica;
                if (!_entities.TryGetValue(snapshot.EntityId, out EntityEntry entry))
                {
                    BaseEntity entity = _clientEntityFactory?.Invoke(snapshot.EntityId, snapshot.OwnerClientId, isOwned);
                    NetworkObjectIdentity identity = entity != null ? entity.GetComponent<NetworkObjectIdentity>() : null;
                    if (entity == null || identity == null) continue;
                    identity.ApplyNetworkMetadata(snapshot.EntityId, snapshot.OwnerClientId, expectedRole);
                    if (!_entities.TryGetValue(snapshot.EntityId, out entry) && !Register(entity, identity, snapshot.OwnerClientId))
                        continue;
                    entry = _entities[snapshot.EntityId];
                }
                else
                {
                    entry.OwnerClientId = snapshot.OwnerClientId;
                    entry.Identity.ApplyNetworkMetadata(snapshot.EntityId, snapshot.OwnerClientId, expectedRole);
                }

                if (snapshot.SnapshotTick <= entry.LastAppliedSnapshotTick) continue;

                EntitySimulationState state = NetSyncUtils.ToSimulationState(snapshot);
                if (!state.IsFinite()) continue;
                if (entry.Identity.Role == EntitySimulationMode.Predict || entry.Identity.Role == EntitySimulationMode.Replica)
                {
                    ApplyTransformState(entry.Entity, state);
                    entry.LastAppliedSnapshotTick = snapshot.SnapshotTick;
                }
            }
        }

        public override void Update(float deltaTime)
        {
            BindNetworkHandlers();
        }

        public override void Destroy()
        {
            if (_networkTime != null) _networkTime.Tick -= SimulateTick;
            if (_clientHandlerBound) _client?.UnRegNetHandler(NetEvent.WORLD_SNAPSHOT);
            if (_serverHandlerBound) _server?.UnRegNetHandler(NetEvent.PLAYER_INPUT);

            _entities.Clear();
            _clientEntityFactory = null;
        }

        private void HandlePlayerInput(uint clientId, global::NetSync.Player_Input input)
        {
            TryAcceptInput(clientId, input, CurrentTick);
        }

        private void SimulateTick(uint tick, float deltaTime)
        {
            AdvanceSimulation(tick, deltaTime);

            _snapshotAccumulator += SyncConfig.Instance.snapshotTickRate;
            if (_snapshotAccumulator < SyncConfig.Instance.simulationTickRate) return;
            _snapshotAccumulator -= SyncConfig.Instance.simulationTickRate;
            BroadcastSnapshots(tick);
        }

        private static void SimulateAuthorityTick(EntityEntry entry, uint tick, float deltaTime)
        {
            entry.Commands.TryDequeueExecutable(tick, out _, out InputState input);
            EntityInputCommand command = entry.CommandBuilder.Build(tick, input);
            entry.Entity.Step(tick, deltaTime, command);
        }

        private void SendOwnedInput(EntityEntry entry, uint tick)
        {
            if (_client == null || !_client.HasFastChannel || entry.OwnerClientId != _client.ClientId) return;

            PlayerController controller = entry.Entity.GetComponent<PlayerController>();
            if (controller == null) return;

            uint inputTick = Math.Max(entry.NextInputTick, entry.LastAppliedSnapshotTick);
            entry.NextInputTick = inputTick + 1;
            InputState input = controller.SampleInput();
            _client.Send(NetEvent.PLAYER_INPUT, NetSyncUtils.ToPlayerInput(entry.Identity.EntityId, inputTick, input));
        }

        private void BroadcastSnapshots(uint tick)
        {
            if (_server == null || !_server.HasFastChannel) return;
            _server.Broadcast(NetEvent.WORLD_SNAPSHOT, CreateWorldSnapshot(tick));
        }

        private void HandleWorldSnapshot(global::NetSync.World_Snapshot world)
        {
            ApplyWorldSnapshot(world, _client?.ClientId ?? 0);
        }

        private static void ApplyTransformState(BaseEntity entity, in EntitySimulationState state)
        {
            entity.GetComponent<MovementModule>()?.Teleport(state.Position);
            entity.GetComponent<RotationModule>()?.Restore(state.Rotation, state.AngularVelocity);
        }

        private static bool IsFinite(Vector2 value)
        {
            return float.IsFinite(value.x) && float.IsFinite(value.y);
        }

        private void BindNetworkHandlers()
        {
            if (!_clientHandlerBound)
            {
                _client = Global.Get<NetClient>();
                if (_client != null)
                {
                    _client.RegNetHandler<global::NetSync.World_Snapshot>(NetEvent.WORLD_SNAPSHOT, HandleWorldSnapshot);
                    _clientHandlerBound = true;
                }
            }

            if (!_serverHandlerBound)
            {
                _server = Global.Get<NetServer>();
                if (_server != null)
                {
                    _server.RegNetHandler<global::NetSync.Player_Input>(NetEvent.PLAYER_INPUT, HandlePlayerInput);
                    _serverHandlerBound = true;
                }
            }
        }
    }
}
