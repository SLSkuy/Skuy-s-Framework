namespace GamePlay.NetSync
{
    /// <summary>
    /// Position/Rotation 变换能力标识。
    /// </summary>
    public sealed class NetworkTransformCapability : NetworkObjectCapabilityBase
    {
        public override NetworkObjectCapabilityId CapabilityId => NetworkObjectCapabilityId.Transform;
        public override NetworkObjectSyncChannelId ChannelId => NetworkObjectSyncChannelId.TransformSnapshot;
    }
}
