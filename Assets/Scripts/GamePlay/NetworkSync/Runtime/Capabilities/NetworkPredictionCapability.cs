namespace GamePlay.NetSync
{
    /// <summary>
    /// 本地预测与权威校正能力标识。
    /// </summary>
    public sealed class NetworkPredictionCapability : NetworkObjectCapabilityBase
    {
        public override NetworkObjectCapabilityId CapabilityId => NetworkObjectCapabilityId.Prediction;
        public override NetworkObjectSyncChannelId ChannelId => NetworkObjectSyncChannelId.InputCommand;
    }
}
