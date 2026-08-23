using System;
using System.Collections.Generic;
using Events;
using Framework;
using GamePlay.EntitySystem;
using Network;
using UnityEngine;
using Utils;

namespace GamePlay.MultiPlaySystem
{
    /// <summary>
    /// 角色同步子系统：显式注册角色并编排输入、权威模拟、预测和插值。
    /// </summary>
    public sealed class CharacterReplicationSystem : SubSystemBase
    {
        private readonly Dictionary<uint, CharacterReplicationEntry> _characters = new();
        private NetworkTimeSystem _networkTime;
        private NetClient _client;
        private NetServer _server;
        private Func<uint, uint, bool, NetworkObjectIdentity> _clientEntityFactory;
        private int _snapshotAccumulator;
        private bool _clientHandlerBound;
        private bool _serverHandlerBound;

        #region Properties
        public override SubSystemPriority Priority => SubSystemPriority.NetSyncManager;
        public uint CurrentTick => _networkTime?.CurrentTick ?? 0;
        public int RegisteredEntityCount => _characters.Count;
        #endregion

        /// <summary>
        /// 注册角色及其所有者。LocalPlay 允许 NetworkObjectId 为 0。
        /// </summary>
        public bool Register(NetworkObjectIdentity identity, uint ownerClientId = 0)
        {
            if (identity == null || (identity.EntityId == 0 && !identity.IsLocalPlay)) return false;
            uint registrationId = GetRegistrationId(identity);
            if (_characters.TryGetValue(registrationId, out CharacterReplicationEntry existingEntry))
            {
                if (existingEntry.Identity != identity)
                {
                    Debug.LogError(
                        $"NetworkObjectId {identity.EntityId} 已被 {existingEntry.Identity.name} 注册，拒绝对象 {identity.name}。");
                    return false;
                }

                existingEntry.OwnerClientId = ownerClientId;
                existingEntry.ApplyRole(identity.Role);
                return true;
            }

            CharacterReplicationEntry entry = new();
            if (!entry.TryBind(identity)) return false;

            entry.OwnerClientId = ownerClientId;
            entry.ApplyRole(identity.Role);
            _characters.Add(registrationId, entry);
            return true;
        }

        /// <summary>
        /// 注册离线 LocalPlay 角色。
        /// </summary>
        public bool RegisterLocalPlay(NetworkObjectIdentity identity)
        {
            return Register(identity, 0);
        }

        /// <summary>
        /// 注销指定实例；相同 ID 的其他实例不会被误移除。
        /// </summary>
        public void Unregister(uint entityId, NetworkObjectIdentity identity)
        {
            uint registrationId = entityId != 0 ? entityId : FindRegistrationId(identity);
            if (registrationId == 0 || !_characters.TryGetValue(registrationId, out CharacterReplicationEntry entry) ||
                entry.Identity != identity)
            {
                return;
            }

            _characters.Remove(registrationId);
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
            if (input == null || !_characters.TryGetValue(input.EntityId, out CharacterReplicationEntry entry))
                return false;
            if (!entry.Identity.IsAuthority || entry.OwnerClientId == 0 || entry.OwnerClientId != clientId)
                return false;

            InputState state = NetSyncUtils.ToInputState(input);
            SyncConfig config = SyncConfig.Instance;
            if (!CharacterInputValidator.TrySanitize(ref state, config.maxInputVectorMagnitude)) return false;

            return entry.Input.Enqueue(input.InputTick, serverTick,
                (uint)config.maxPastInputTicks, (uint)config.maxFutureInputTicks, state);
        }

        /// <summary>
        /// 推进所有权威角色与本地预测角色的固定 Tick。
        /// </summary>
        public void AdvanceTick(uint tick, float deltaTime)
        {
            BindNetworkHandlers();
            foreach (CharacterReplicationEntry entry in _characters.Values)
            {
                if (entry.Identity.IsLocalPlay) SimulateLocalPlayTick(entry, tick, deltaTime);
                else if (entry.Identity.IsPredict) PredictOwnedTick(entry, tick, deltaTime);
                else if (entry.Identity.IsAuthority) SimulateAuthorityTick(entry, tick, deltaTime);
            }
        }

        private static void SimulateLocalPlayTick(CharacterReplicationEntry entry, uint tick, float deltaTime)
        {
            if (entry.PlayerController == null) return;
            InputState input = entry.PlayerController.SampleInput();
            EntityInputCommand command = entry.Input.BuildPredictedCommand(tick, input);
            entry.Simulation.Step(tick, deltaTime, command);
        }

        [Obsolete("Obsolete")]
        private static uint GetRegistrationId(NetworkObjectIdentity identity)
        {
            if (identity.EntityId != 0) return identity.EntityId;
            return unchecked((uint)identity.GetInstanceID());
        }

        private uint FindRegistrationId(NetworkObjectIdentity identity)
        {
            foreach (KeyValuePair<uint, CharacterReplicationEntry> pair in _characters)
            {
                if (pair.Value.Identity == identity) return pair.Key;
            }
            return 0;
        }

        /// <summary>
        /// 捕获当前权威世界快照。
        /// </summary>
        public global::NetSync.World_Snapshot CreateWorldSnapshot(uint tick)
        {
            global::NetSync.World_Snapshot world = new() { SnapshotTick = tick };
            foreach (CharacterReplicationEntry entry in _characters.Values)
            {
                if (!entry.Identity.IsAuthority) continue;
                EntitySimulationState state = entry.Presentation.CaptureState();
                if (!state.IsFinite()) continue;
                world.CharacterSnapshots.Add(NetSyncUtils.ToCharacterSnapshotMessage(
                    state,
                    entry.Identity.NetworkObjectId,
                    entry.OwnerClientId,
                    tick,
                    entry.Input.LastProcessedTick));
            }

            return world;
        }

        /// <summary>
        /// 应用权威世界快照，并依据 Owner 元数据选择 Predict 或 Replica。
        /// </summary>
        public void ApplyWorldSnapshot(global::NetSync.World_Snapshot world, uint localClientId)
        {
            if (world == null) return;
            foreach (global::NetSync.Character_Snapshot snapshot in world.CharacterSnapshots)
            {
                EntitySimulationState state = NetSyncUtils.ToSimulationState(snapshot);
                if (!state.IsFinite()) continue;

                if (_characters.TryGetValue(snapshot.EntityId, out CharacterReplicationEntry existingEntry) &&
                    snapshot.SnapshotTick <= existingEntry.LastAppliedSnapshotTick)
                {
                    continue;
                }

                bool isOwned = snapshot.OwnerClientId != 0 && snapshot.OwnerClientId == localClientId;
                EntitySimulationMode expectedRole = isOwned ? EntitySimulationMode.Predict : EntitySimulationMode.Replica;
                CharacterReplicationEntry entry = ResolveSnapshotEntry(snapshot, expectedRole, isOwned);
                if (entry == null) continue;

                entry.OwnerClientId = snapshot.OwnerClientId;
                if (entry.Identity.IsPredict)
                {
                    entry.Prediction.SetSnapshotAnchor(snapshot.SnapshotTick);
                    ReconcilePredictedCharacter(entry, snapshot, state, GetTickDeltaTime());
                }
                else if (entry.Identity.IsReplica)
                {
                    if (entry.Interpolation.AddSnapshot(snapshot.SnapshotTick, state))
                    {
                        entry.Presentation.ApplyState(state);
                    }
                }

                entry.LastAppliedSnapshotTick = snapshot.SnapshotTick;
            }
        }

        private CharacterReplicationEntry ResolveSnapshotEntry(global::NetSync.Character_Snapshot snapshot,
            EntitySimulationMode expectedRole, bool isOwned)
        {
            if (!_characters.TryGetValue(snapshot.EntityId, out CharacterReplicationEntry entry))
            {
                NetworkObjectIdentity identity =
                    _clientEntityFactory?.Invoke(snapshot.EntityId, snapshot.OwnerClientId, isOwned);
                if (identity == null) return null;
                identity.ApplyNetworkMetadata(snapshot.EntityId, snapshot.OwnerClientId, expectedRole);
                if (!_characters.TryGetValue(snapshot.EntityId, out entry) &&
                    !Register(identity, snapshot.OwnerClientId))
                {
                    return null;
                }

                entry = _characters[snapshot.EntityId];
            }
            else
            {
                entry.Identity.ApplyNetworkMetadata(snapshot.EntityId, snapshot.OwnerClientId, expectedRole);
            }

            return entry;
        }

        private static void SimulateAuthorityTick(CharacterReplicationEntry entry, uint tick, float deltaTime)
        {
            EntityInputCommand command = entry.Input.BuildAuthorityCommand(tick);
            entry.Simulation.Step(tick, deltaTime, command);
        }

        private void PredictOwnedTick(CharacterReplicationEntry entry, uint tick, float deltaTime)
        {
            BindNetworkHandlers();
            if (_client == null || entry.OwnerClientId != _client.ClientId || entry.PlayerController == null)
            {
                return;
            }

            uint inputTick = entry.Prediction.AllocateInputTick(tick);
            InputState input = entry.PlayerController.SampleInput();
            SyncConfig config = SyncConfig.Instance;
            if (!CharacterInputValidator.TrySanitize(ref input, config.maxInputVectorMagnitude))
            {
                input = default;
            }

            EntityInputCommand command = entry.Input.BuildPredictedCommand(inputTick, input);
            entry.Simulation.Step(inputTick, deltaTime, command);
            entry.Prediction.History.Add(new EntityPredictionFrame
            {
                Tick = inputTick,
                Command = command,
                State = entry.Simulation.CaptureRollbackState()
            });

            global::NetSync.Player_Input message =
                NetSyncUtils.ToPlayerInput(entry.Identity.EntityId, inputTick, input);
            if (_client.HasFastChannel) _client.Send(NetEvent.PLAYER_INPUT, message);
            else if (_client.IsRunning) _client.SendReliable(NetEvent.PLAYER_INPUT, message);
        }

        private static void ReconcilePredictedCharacter(CharacterReplicationEntry entry,
            global::NetSync.Character_Snapshot snapshot,
            in EntitySimulationState authoritativeState,
            float tickDeltaTime)
        {
            CharacterPredictionController prediction = entry.Prediction;
            uint confirmedTick = snapshot.LastProcessedInputTick;
            if (confirmedTick == 0)
            {
                if (prediction.History.Count == 0)
                {
                    ApplyCorrectedTransform(entry, authoritativeState,
                        ShouldSmoothCorrection(entry.Presentation.CaptureState(), authoritativeState));
                }

                return;
            }

            prediction.Confirm(confirmedTick);
            if (!prediction.History.TryGet(confirmedTick, out EntityPredictionFrame confirmedFrame))
            {
                ApplyCorrectedTransform(entry, authoritativeState,
                    ShouldSmoothCorrection(entry.Presentation.CaptureState(), authoritativeState));
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
            entry.Presentation.BeginPredictionCorrection();

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

            entry.Presentation.EndPredictionCorrection(smoothCorrection);
            prediction.History.RemoveThrough(confirmedTick);
        }

        private static void ApplyCorrectedTransform(CharacterReplicationEntry entry,
            in EntitySimulationState authoritativeState, bool smoothCorrection)
        {
            entry.Presentation.BeginPredictionCorrection();
            entry.Presentation.ApplyState(authoritativeState);
            entry.Presentation.EndPredictionCorrection(smoothCorrection);
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
            foreach (CharacterReplicationEntry entry in _characters.Values)
            {
                if (!entry.Identity.IsReplica) continue;
                if (!entry.Interpolation.TrySample(deltaTime, out EntitySimulationState state)) continue;
                entry.Presentation.ApplyState(state);
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
            AdvanceTick(tick, deltaTime);

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

        #region Subsystem Lifecycle

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

            _characters.Clear();
            _clientEntityFactory = null;
        }

        #endregion
    }
}
