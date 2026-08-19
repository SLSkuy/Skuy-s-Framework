namespace GamePlay.NetSync
{
    /// <summary>
    /// 世界快照生产与消费能力标识。
    /// </summary>
    public sealed class NetworkSnapshotCapability : NetworkObjectCapabilityBase
    {
        public override NetworkObjectCapabilityId CapabilityId => NetworkObjectCapabilityId.Snapshot;
        public override NetworkObjectSyncChannelId ChannelId => NetworkObjectSyncChannelId.TransformSnapshot;
    }
}
