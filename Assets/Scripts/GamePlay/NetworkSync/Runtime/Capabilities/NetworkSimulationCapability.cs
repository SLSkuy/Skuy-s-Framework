using System;
using System.Collections.Generic;
using GamePlay.EntitySystem;

namespace GamePlay.NetSync
{
    /// <summary>
    /// 固定 Tick 实体模拟与回滚状态能力。
    /// </summary>
    public sealed class NetworkSimulationCapability : NetworkObjectCapabilityBase
    {
        private static readonly NetworkObjectCapabilityId[] Dependencies =
        {
            NetworkObjectCapabilityId.Transform
        };

        private EntitySimulationObject _entity;

        #region 属性
        public override NetworkObjectCapabilityId CapabilityId => NetworkObjectCapabilityId.Simulation;
        public override NetworkObjectSyncChannelId ChannelId => NetworkObjectSyncChannelId.SimulationState;
        public override IReadOnlyList<NetworkObjectCapabilityId> RequiredCapabilities => Dependencies;
        #endregion

        /// <inheritdoc />
        public override bool SupportsMode(EntitySimulationMode mode) => mode != EntitySimulationMode.Replica;

        /// <summary>
        /// 推进一次固定 Tick 模拟。
        /// </summary>
        public void Step(uint tick, float deltaTime, in EntityInputCommand command)
        {
            _entity.Step(tick, deltaTime, command);
        }

        /// <summary>
        /// 捕获当前完整回滚状态。
        /// </summary>
        public EntityRollbackState CaptureRollbackState()
        {
            return _entity.CaptureRollbackState();
        }

        /// <summary>
        /// 恢复完整回滚状态。
        /// </summary>
        public void RestoreRollbackState(in EntityRollbackState state)
        {
            _entity.RestoreRollbackState(state);
        }

        protected override void OnActivated(EntitySimulationMode mode)
        {
            _entity = GetComponent<EntitySimulationObject>();
            if (_entity == null)
            {
                    throw new InvalidOperationException($"网络对象 {name} 声明了 Simulation 能力，但缺少 EntitySimulationObject。 ");
            }

            if (!_entity.IsInitialized) _entity.Init();
            GetComponent<MovementModule>()?.SetReplicaMode(false);
        }

        protected override void OnDeactivated()
        {
            _entity = null;
        }
    }
}
