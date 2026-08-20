using System.Collections.Generic;
using GamePlay.EntitySystem;
using UnityEngine;

namespace GamePlay.NetSync
{
    /// <summary>
    /// 远端 Transform 快照缓冲、传送检测与插值能力。
    /// </summary>
    public sealed class NetworkInterpolationCapability : NetworkObjectCapabilityBase
    {
        private static readonly NetworkObjectCapabilityId[] Dependencies =
        {
            NetworkObjectCapabilityId.Transform,
            NetworkObjectCapabilityId.Snapshot
        };

        private SnapshotInterpolator _interpolator;
        private EntitySimulationState _lastIncomingState;
        private bool _hasIncomingState;

        #region 属性
        public override NetworkObjectCapabilityId CapabilityId => NetworkObjectCapabilityId.Interpolation;
        public override NetworkObjectSyncChannelId ChannelId => NetworkObjectSyncChannelId.TransformSnapshot;
        public override IReadOnlyList<NetworkObjectCapabilityId> RequiredCapabilities => Dependencies;
        #endregion

        /// <inheritdoc />
        public override bool SupportsMode(EntitySimulationMode mode) => mode == EntitySimulationMode.Replica;

        /// <summary>
        /// 添加远端快照；首帧或大距离传送返回 true，调用方应立即 Snap。
        /// </summary>
        public bool AddSnapshot(uint snapshotTick, in EntitySimulationState state)
        {
            SyncConfig config = SyncConfig.Instance;
            bool requiresSnap = !_hasIncomingState ||
                Vector3.Distance(_lastIncomingState.Position, state.Position) >= config.positionSnapThreshold ||
                Quaternion.Angle(_lastIncomingState.Rotation, state.Rotation) >=
                config.rotationSnapThresholdDegrees;
            if (requiresSnap) _interpolator.Reset();

            _interpolator.Add(snapshotTick, state);
            _lastIncomingState = state;
            _hasIncomingState = true;
            return requiresSnap;
        }

        /// <summary>
        /// 推进远端渲染时间并采样 Transform。
        /// </summary>
        public bool TrySample(float deltaTime, out EntitySimulationState state)
        {
            return _interpolator.TrySample(deltaTime, out state);
        }

        protected override void OnActivated(EntitySimulationMode mode)
        {
            SyncConfig config = SyncConfig.Instance;
            _interpolator ??= new SnapshotInterpolator(
                config.simulationTickRate,
                config.interpolationDelayTicks,
                Mathf.Max(8, config.interpolationDelayTicks * 4));
            ResetState();
        }

        protected override void OnDeactivated()
        {
            ResetState();
        }

        private void ResetState()
        {
            _interpolator?.Reset();
            _lastIncomingState = default;
            _hasIncomingState = false;
        }
    }
}
