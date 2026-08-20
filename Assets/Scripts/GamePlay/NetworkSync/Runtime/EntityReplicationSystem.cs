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
    /// 网络实体复制系统：编排已激活能力的输入、模拟、快照、预测和插值流程。
    /// </summary>
    public sealed class EntityReplicationSystem : SubSystemBase
    {
        private sealed class EntityEntry
        {
            public NetworkObjectIdentity Identity;
            public NetworkTransformCapability Transform;
            public NetworkInputCapability Input;
            public NetworkSimulationCapability Simulation;
            public NetworkSnapshotCapability Snapshot;
            public NetworkPredictionCapability Prediction;
            public NetworkInterpolationCapability Interpolation;
            public uint OwnerClientId;
            public uint LastAppliedSnapshotTick;

            public bool RefreshCapabilities()
            {
                Identity.TryGetCapability(out Transform);
                Identity.TryGetCapability(out Input);
                Identity.TryGetCapability(out Simulation);
                Identity.TryGetCapability(out Snapshot);
                Identity.TryGetCapability(out Prediction);
                Identity.TryGetCapability(out Interpolation);
                return Transform != null && Snapshot != null;
            }
        }

        private readonly Dictionary<uint, EntityEntry> _entities = new();
        private NetworkTimeSystem _networkTime;
        private NetClient _client;
        private NetServer _server;
        private Func<uint, uint, bool, NetworkObjectIdentity> _clientEntityFactory;
        private int _snapshotAccumulator;
        private bool _clientHandlerBound;
        private bool _serverHandlerBound;

        #region 属性

        public override SubSystemPriority Priority => SubSystemPriority.NetSyncManager;
        public uint CurrentTick => _networkTime?.CurrentTick ?? 0;
        public int RegisteredEntityCount => _entities.Count;

        #endregion

        /// <summary>
        /// 注册网络对象及其所有者。
        /// </summary>
        public bool Register(NetworkObjectIdentity identity, uint ownerClientId = 0)
        {
            if (identity == null || identity.EntityId == 0) return false;
            if (_entities.TryGetValue(identity.EntityId, out EntityEntry existingEntry))
            {
                if (existingEntry.Identity != identity)
                {
                    Debug.LogError(
                        $"NetworkObjectId {identity.EntityId} 已被 {existingEntry.Identity.name} 注册，拒绝对象 {identity.name}。");
                    return false;
                }

                existingEntry.OwnerClientId = ownerClientId;
                return existingEntry.RefreshCapabilities();
            }

            EntityEntry entry = new()
            {
                Identity = identity,
                OwnerClientId = ownerClientId
            };
            if (!entry.RefreshCapabilities())
            {
                Debug.LogError($"网络对象 {identity.name} 缺少 Transform 或 Snapshot 能力，无法注册复制系统。");
                return false;
            }

            _entities.Add(identity.EntityId, entry);
            return true;
        }

        /// <summary>
        /// 注销指定实例；相同 ID 的其他实例不会被误移除。
        /// </summary>
        public void Unregister(uint entityId, NetworkObjectIdentity identity)
        {
            if (!_entities.TryGetValue(entityId, out EntityEntry entry) || entry.Identity != identity) return;
            _entities.Remove(entityId);
        }

        /// <summary>
        /// 配置客户端对象工厂，参数依次为对象 ID、Owner ID 和是否本地拥有。
        /// </summary>
        public void SetClientEntityFactory(Func<uint, uint, bool, NetworkObjectIdentity> entityFactory)
        {
            _clientEntityFactory = entityFactory;
        }

        /// <summary>
        /// 接收并校验一个客户端输入。该入口供传输层与集成测试共同使用。
        /// </summary>
        public bool TryAcceptInput(uint clientId, global::NetSync.Player_Input input, uint serverTick)
        {
            if (input == null || !_entities.TryGetValue(input.EntityId, out EntityEntry entry)) return false;
            if (!entry.Identity.IsAuthority || entry.OwnerClientId != clientId ||
                entry.Input == null || !entry.Input.IsActive ||
                entry.Simulation == null || !entry.Simulation.IsActive)
            {
                return false;
            }

            SyncConfig config = SyncConfig.Instance;
            InputState state = NetSyncUtils.ToInputState(input);
            if (!IsFinite(state.MoveInput) || !IsFinite(state.AimInput)) return false;
            float maxMagnitudeSquared = config.maxInputVectorMagnitude * config.maxInputVectorMagnitude;
            if (state.MoveInput.sqrMagnitude > maxMagnitudeSquared || state.AimInput.sqrMagnitude > maxMagnitudeSquared)
                return false;

            return entry.Input.Enqueue(input.InputTick, serverTick,
                (uint)config.maxPastInputTicks, (uint)config.maxFutureInputTicks, state);
        }

        /// <summary>
        /// 推进所有当前启用的权威模拟或预测能力。
        /// </summary>
        public void AdvanceSimulation(uint tick, float deltaTime)
        {
            foreach (EntityEntry entry in _entities.Values)
            {
                if (entry.Simulation == null || !entry.Simulation.IsActive) continue;
                if (entry.Prediction != null && entry.Prediction.IsActive)
                {
                    PredictOwnedTick(entry, tick, deltaTime);
                }
                else if (entry.Identity.IsAuthority && entry.Input != null && entry.Input.IsActive)
                {
                    SimulateAuthorityTick(entry, tick, deltaTime);
                }
            }
        }

        /// <summary>
        /// 捕获当前权威世界快照。只有启用 Snapshot 与 Transform 的对象参与。
        /// </summary>
        public global::NetSync.World_Snapshot CreateWorldSnapshot(uint tick)
        {
            global::NetSync.World_Snapshot world = new() { SnapshotTick = tick };
            foreach (EntityEntry entry in _entities.Values)
            {
                if (!entry.Identity.IsAuthority || !entry.Transform.IsActive || !entry.Snapshot.IsActive) continue;
                EntitySimulationState state = entry.Transform.CaptureState();
                if (!state.IsFinite()) continue;
                world.TransformSnapshots.Add(NetSyncUtils.ToTransformSnapshotMessage(
                    state,
                    entry.Identity.NetworkObjectId,
                    entry.OwnerClientId,
                    tick,
                    entry.Input?.LastProcessedTick ?? 0));
            }

            return world;
        }

        /// <summary>
        /// 应用权威世界快照，并依据 Owner 元数据选择 Predict 或 Replica 能力矩阵。
        /// </summary>
        public void ApplyWorldSnapshot(global::NetSync.World_Snapshot world, uint localClientId)
        {
            if (world == null) return;
            foreach (global::NetSync.Transform_Snapshot snapshot in world.TransformSnapshots)
            {
                EntitySimulationState state = NetSyncUtils.ToSimulationState(snapshot);
                if (!state.IsFinite()) continue;

                if (_entities.TryGetValue(snapshot.EntityId, out EntityEntry existingEntry) &&
                    snapshot.SnapshotTick <= existingEntry.LastAppliedSnapshotTick)
                {
                    continue;
                }

                bool isOwned = snapshot.OwnerClientId != 0 && snapshot.OwnerClientId == localClientId;
                EntitySimulationMode expectedRole = isOwned ? EntitySimulationMode.Predict : EntitySimulationMode.Replica;
                EntityEntry entry = ResolveSnapshotEntry(snapshot, expectedRole, isOwned);
                if (entry == null) continue;

                entry.OwnerClientId = snapshot.OwnerClientId;
                entry.Prediction?.SetSnapshotAnchor(snapshot.SnapshotTick);
                if (entry.Prediction != null && entry.Prediction.IsActive)
                {
                    ReconcilePredictedEntity(entry, snapshot, state, GetTickDeltaTime());
                }
                else if (entry.Interpolation != null && entry.Interpolation.IsActive)
                {
                    if (entry.Interpolation.AddSnapshot(snapshot.SnapshotTick, state))
                    {
                        entry.Transform.ApplyState(state);
                    }
                }

                entry.LastAppliedSnapshotTick = snapshot.SnapshotTick;
            }
        }

        private EntityEntry ResolveSnapshotEntry(global::NetSync.Transform_Snapshot snapshot,
            EntitySimulationMode expectedRole, bool isOwned)
        {
            if (!_entities.TryGetValue(snapshot.EntityId, out EntityEntry entry))
            {
                NetworkObjectIdentity identity =
                    _clientEntityFactory?.Invoke(snapshot.EntityId, snapshot.OwnerClientId, isOwned);
                if (identity == null) return null;
                identity.ApplyNetworkMetadata(snapshot.EntityId, snapshot.OwnerClientId, expectedRole);
                if (!_entities.TryGetValue(snapshot.EntityId, out entry) &&
                    !Register(identity, snapshot.OwnerClientId))
                {
                    return null;
                }
                entry = _entities[snapshot.EntityId];
            }
            else
            {
                entry.Identity.ApplyNetworkMetadata(snapshot.EntityId, snapshot.OwnerClientId, expectedRole);
                entry.RefreshCapabilities();
            }

            return entry;
        }

        private static void SimulateAuthorityTick(EntityEntry entry, uint tick, float deltaTime)
        {
            EntityInputCommand command = entry.Input.BuildAuthorityCommand(tick);
            entry.Simulation.Step(tick, deltaTime, command);
        }

        private void PredictOwnedTick(EntityEntry entry, uint tick, float deltaTime)
        {
            if (_client == null || entry.OwnerClientId != _client.ClientId ||
                entry.Input == null || !entry.Input.IsActive)
            {
                return;
            }

            uint inputTick = entry.Prediction.AllocateInputTick(tick);
            EntityInputCommand command = entry.Input.BuildPredictedCommand(inputTick, out InputState input);
            entry.Simulation.Step(inputTick, deltaTime, command);
            entry.Prediction.History.Add(new EntityPredictionFrame
            {
                Tick = inputTick,
                Command = command,
                State = entry.Simulation.CaptureRollbackState()
            });
            if (_client.HasFastChannel)
            {
                _client.Send(NetEvent.PLAYER_INPUT,
                    NetSyncUtils.ToPlayerInput(entry.Identity.EntityId, inputTick, input));
            }
        }

        private static void ReconcilePredictedEntity(EntityEntry entry,
            global::NetSync.Transform_Snapshot snapshot,
            in EntitySimulationState authoritativeState,
            float tickDeltaTime)
        {
            NetworkPredictionCapability prediction = entry.Prediction;
            uint confirmedTick = snapshot.LastProcessedInputTick;
            if (confirmedTick == 0)
            {
                if (prediction.History.Count == 0)
                {
                    ApplyCorrectedTransform(entry, authoritativeState,
                        ShouldSmoothCorrection(entry.Transform.CaptureState(), authoritativeState));
                }
                return;
            }

            prediction.Confirm(confirmedTick);
            if (!prediction.History.TryGet(confirmedTick, out EntityPredictionFrame confirmedFrame))
            {
                ApplyCorrectedTransform(entry, authoritativeState,
                    ShouldSmoothCorrection(entry.Transform.CaptureState(), authoritativeState));
                prediction.History.Clear();
                prediction.AdvanceAfter(confirmedTick);
                return;
            }

            EntitySimulationState predictedState = confirmedFrame.State.TransformState;
            SyncConfig config = SyncConfig.Instance;
            float positionError = Vector3.Distance(predictedState.Position, authoritativeState.Position);
            float rotationError = Quaternion.Angle(predictedState.Rotation, authoritativeState.Rotation);
            prediction.History.CopyAfter(confirmedTick, prediction.ReplayFrames);

            if (positionError <= config.positionReconcileThreshold &&
                rotationError <= config.rotationReconcileThresholdDegrees)
            {
                prediction.History.RemoveThrough(confirmedTick);
                return;
            }

            bool smoothCorrection = positionError < config.positionSnapThreshold &&
                rotationError < config.rotationSnapThresholdDegrees;
            entry.Transform.BeginPredictionCorrection();

            EntityRollbackState rollbackState = confirmedFrame.State;
            rollbackState.TransformState = authoritativeState;
            rollbackState.MovementState.LinearVelocity = authoritativeState.LinearVelocity;
            entry.Simulation.RestoreRollbackState(rollbackState);

            for (int i = 0; i < prediction.ReplayFrames.Count; i++)
            {
                EntityPredictionFrame frame = prediction.ReplayFrames[i];
                entry.Simulation.Step(frame.Tick, tickDeltaTime, frame.Command);
                frame.State = entry.Simulation.CaptureRollbackState();
                prediction.History.Add(frame);
            }

            entry.Transform.EndPredictionCorrection(smoothCorrection);
            prediction.History.RemoveThrough(confirmedTick);
        }

        private static void ApplyCorrectedTransform(EntityEntry entry,
            in EntitySimulationState authoritativeState, bool smoothCorrection)
        {
            entry.Transform.BeginPredictionCorrection();
            entry.Transform.ApplyState(authoritativeState);
            entry.Transform.EndPredictionCorrection(smoothCorrection);
        }

        private static bool ShouldSmoothCorrection(in EntitySimulationState currentState,
            in EntitySimulationState authoritativeState)
        {
            SyncConfig config = SyncConfig.Instance;
            return Vector3.Distance(currentState.Position, authoritativeState.Position) < config.positionSnapThreshold &&
                Quaternion.Angle(currentState.Rotation, authoritativeState.Rotation) <
                config.rotationSnapThresholdDegrees;
        }

        private void UpdateReplicaInterpolation(float deltaTime)
        {
            foreach (EntityEntry entry in _entities.Values)
            {
                if (entry.Interpolation == null || !entry.Interpolation.IsActive) continue;
                if (!entry.Interpolation.TrySample(deltaTime, out EntitySimulationState state)) continue;
                entry.Transform.ApplyState(state);
            }
        }

        private void HandlePlayerInput(uint clientId, global::NetSync.Player_Input input)
        {
            TryAcceptInput(clientId, input, CurrentTick);
        }

        private void HandleWorldSnapshot(global::NetSync.World_Snapshot world)
        {
            ApplyWorldSnapshot(world, _client?.ClientId ?? 0);
        }

        private void SimulateTick(uint tick, float deltaTime)
        {
            AdvanceSimulation(tick, deltaTime);

            _snapshotAccumulator += SyncConfig.Instance.snapshotTickRate;
            if (_snapshotAccumulator < SyncConfig.Instance.simulationTickRate) return;
            _snapshotAccumulator -= SyncConfig.Instance.simulationTickRate;
            BroadcastSnapshots(tick);
        }

        private void BroadcastSnapshots(uint tick)
        {
            if (_server == null || !_server.HasFastChannel) return;
            _server.Broadcast(NetEvent.WORLD_SNAPSHOT, CreateWorldSnapshot(tick));
        }

        private float GetTickDeltaTime()
        {
            return SyncConfig.Instance.simulationTickRate > 0 ?
                1f / SyncConfig.Instance.simulationTickRate : 0f;
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
                    _client.RegNetHandler<global::NetSync.World_Snapshot>(
                        NetEvent.WORLD_SNAPSHOT, HandleWorldSnapshot);
                    _clientHandlerBound = true;
                }
            }

            if (!_serverHandlerBound)
            {
                _server = Global.Get<NetServer>();
                if (_server != null)
                {
                    _server.RegNetHandler<global::NetSync.Player_Input>(
                        NetEvent.PLAYER_INPUT, HandlePlayerInput);
                    _serverHandlerBound = true;
                }
            }
        }

        #region 子系统生命周期

        public override void Init()
        {
            _networkTime = Global.Get<NetworkTimeSystem>();
            if (_networkTime != null) _networkTime.Tick += SimulateTick;
            BindNetworkHandlers();
        }

        public override void Update(float deltaTime)
        {
            BindNetworkHandlers();
            UpdateReplicaInterpolation(deltaTime);
        }

        public override void Destroy()
        {
            if (_networkTime != null) _networkTime.Tick -= SimulateTick;
            if (_clientHandlerBound) _client?.UnRegNetHandler(NetEvent.WORLD_SNAPSHOT);
            if (_serverHandlerBound) _server?.UnRegNetHandler(NetEvent.PLAYER_INPUT);

            _entities.Clear();
            _clientEntityFactory = null;
        }

        #endregion
    }
}
