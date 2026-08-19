using GamePlay.EntitySystem;

namespace GamePlay.NetSync
{
    /// <summary>
    /// 网络对象可组合能力的最小生命周期接口。
    /// </summary>
    public interface INetworkObjectCapability
    {
        NetworkObjectCapabilityId CapabilityId { get; }
        NetworkObjectSyncChannelId ChannelId { get; }
        bool IsActive { get; }
        void Activate(NetworkObjectIdentity identity, EntitySimulationMode mode);
        void Deactivate();
    }
}
