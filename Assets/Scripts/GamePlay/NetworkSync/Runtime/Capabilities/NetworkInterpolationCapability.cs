namespace GamePlay.NetSync
{
    /// <summary>
    /// 远端快照插值表现能力标识。
    /// </summary>
    public sealed class NetworkInterpolationCapability : NetworkObjectCapabilityBase
    {
        public override NetworkObjectCapabilityId CapabilityId => NetworkObjectCapabilityId.Interpolation;
        public override NetworkObjectSyncChannelId ChannelId => NetworkObjectSyncChannelId.TransformSnapshot;
    }
}
