using System.Collections.Generic;
using GamePlay.EntitySystem;

namespace GamePlay.NetSync
{
    /// <summary>
    /// 世界快照生产与消费能力标识。
    /// </summary>
    public sealed class NetworkSnapshotCapability : NetworkObjectCapabilityBase
    {
        private static readonly NetworkObjectCapabilityId[] Dependencies =
        {
            NetworkObjectCapabilityId.Transform
        };

        #region 属性
        public override NetworkObjectCapabilityId CapabilityId => NetworkObjectCapabilityId.Snapshot;
        public override NetworkObjectSyncChannelId ChannelId => NetworkObjectSyncChannelId.TransformSnapshot;
        public override IReadOnlyList<NetworkObjectCapabilityId> RequiredCapabilities => Dependencies;
        #endregion

        /// <inheritdoc />
        public override bool SupportsMode(EntitySimulationMode mode) => mode != EntitySimulationMode.LocalPlay;
    }
}
