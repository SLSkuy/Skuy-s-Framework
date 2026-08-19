namespace GamePlay.NetSync
{
    /// <summary>
    /// 固定 Tick 实体模拟能力标识。
    /// </summary>
    public sealed class NetworkSimulationCapability : NetworkObjectCapabilityBase
    {
        public override NetworkObjectCapabilityId CapabilityId => NetworkObjectCapabilityId.Simulation;
        public override NetworkObjectSyncChannelId ChannelId => NetworkObjectSyncChannelId.SimulationState;
    }
}
