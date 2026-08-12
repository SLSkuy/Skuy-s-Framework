using System;
using GamePlay.NetSync;
using UnityEngine;

namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 位置同步模块，负责位置快照的捕获、回放与插值，不直接挂载到 GameObject。
    /// </summary>
    public class NetPositionSync : INetSyncComponent,
        INetSyncSnapshotSource<NetPositionSnapshot>,
        INetSyncSnapshotReceiver<NetPositionSnapshot>
    {
        private readonly MovementModule _movementModule;
        private NetEntitySyncRoot _syncRoot;

        private SnapshotBuffer<NetPositionSnapshot> _snapshots;
        private Vector3 _lastCapturedPosition;
        private double _simulationTickInterval;
        private double _renderServerTime;
        private int _interpolationDelayTicks;
        private uint _lastCapturedTick;
        private bool _hasRenderTick;
        private bool _hasCapturedSnapshot;

        #region 属性
        public ModuleType ModuleType => ModuleType.Position;
        
        public Vector3 CurrentRenderVelocity { get; private set; }
        #endregion

        public NetPositionSync(MovementModule movementModule)
        {
            _movementModule = movementModule;
        }
        
        /// <summary>
        /// 绑定同步根对象。
        /// </summary>
        public void Bind(NetEntitySyncRoot syncRoot)
        {
            _syncRoot = syncRoot;
        }

        /// <summary>
        /// 根据网络角色配置同步行为。
        /// </summary>
        public void ConfigureRole(NetEntityRole role)
        {
            if (_movementModule)
            {
                _movementModule.SetReplicaMode(role == NetEntityRole.Replica);
            }

            if (role == NetEntityRole.Replica)
            {
                SyncConfig config = SyncConfig.Instance;
                if (_snapshots == null)
                {
                    _simulationTickInterval = 1d / Mathf.Max(1, config.simulationTickRate);
                    _interpolationDelayTicks = Mathf.Max(1, config.interpolationDelayTicks);

                    int capacity = Mathf.Max(8, _interpolationDelayTicks * 4);
                    _snapshots = new SnapshotBuffer<NetPositionSnapshot>(capacity);
                }
            }
            else
            {
                _snapshots?.Clear();
                _hasRenderTick = false;
            }
        }
        
        /// <summary>
        /// 执行插值更新。
        /// </summary>
        public void Update(float deltaTime)
        {
            UpdateInterpolation(deltaTime);
        }
        
        /// <summary>
        /// 捕获当前快照。
        /// </summary>
        public NetPositionSnapshot CaptureSnapshot(uint snapshotTick, uint lastProcessedInputTick = 0)
        {
            Vector3 position = _movementModule != null ? _movementModule.Position : Vector3.zero;
            Vector3 velocity = Vector3.zero;
            if (_hasCapturedSnapshot && snapshotTick > _lastCapturedTick)
            {
                float deltaTime = (snapshotTick - _lastCapturedTick) * (float)_simulationTickInterval;
                if (deltaTime > Mathf.Epsilon)
                {
                    velocity = (position - _lastCapturedPosition) / deltaTime;
                }
            }

            _lastCapturedPosition = position;
            _lastCapturedTick = snapshotTick;
            _hasCapturedSnapshot = true;

            return new NetPositionSnapshot
            {
                EntityId = _syncRoot != null ? _syncRoot.EntityId : 0,
                SnapshotTick = snapshotTick,
                LastProcessedInputTick = lastProcessedInputTick,
                Position = position,
                Velocity = velocity,
            };
        }

        /// <summary>
        /// 应用权威快照。
        /// </summary>
        public void ApplySnapshot(in NetPositionSnapshot snapshot)
        {
            if (_movementModule == null) return;
            _movementModule.Teleport(snapshot.Position);
            CurrentRenderVelocity = snapshot.Velocity;
        }

        /// <summary>
        /// 接收权威快照，仅 Replica 使用。
        /// </summary>
        public void OnAuthoritySnapshot(in NetPositionSnapshot snapshot)
        {
            if (!_syncRoot || !_syncRoot.IsReplica) return;
            if (_syncRoot.IsInitialized && snapshot.EntityId != _syncRoot.EntityId) return;
            if (_snapshots == null) return;

            _snapshots.Add(snapshot);

            if (_hasRenderTick) return;

            _renderServerTime = snapshot.SnapshotTick * _simulationTickInterval;
            _hasRenderTick = true;
            ApplySnapshot(snapshot);
        }
        
        /// <summary>
        /// 插值更新。仅 Replica 使用。
        /// </summary>
        public void UpdateInterpolation(float deltaTime)
        {
            if (!_syncRoot || !_syncRoot.IsReplica) return;
            if (!_hasRenderTick || _snapshots == null || _snapshots.Count < 2) return;

            float bufferedTickSpan = _snapshots.LatestTick - _snapshots.OldestTick;
            if (bufferedTickSpan < _interpolationDelayTicks) return;

            double targetRenderTime = (_snapshots.LatestTick - _interpolationDelayTicks) * _simulationTickInterval;
            _renderServerTime = Math.Min(_renderServerTime + deltaTime, targetRenderTime);

            if (_snapshots.TrySample(_renderServerTime, _simulationTickInterval, out NetPositionSnapshot from, out NetPositionSnapshot to, out float t))
            {
                ApplyInterpolatedSnapshot(from, to, t);
            }
        }

        /// <summary>
        /// 应用插值快照。
        /// </summary>
        private void ApplyInterpolatedSnapshot(in NetPositionSnapshot from, in NetPositionSnapshot to, float t)
        {
            if (!_movementModule) return;
            Vector3 position = Vector3.Lerp(from.Position, to.Position, t);
            _movementModule.Teleport(position);
            CurrentRenderVelocity = Vector3.Lerp(from.Velocity, to.Velocity, t);
        }
    }
}
