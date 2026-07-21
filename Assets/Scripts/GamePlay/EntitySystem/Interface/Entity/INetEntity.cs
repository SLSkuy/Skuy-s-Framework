namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 网络实体只描述身份和当前的模拟角色
    /// </summary>
    public interface INetEntity
    {
        uint EntityId { get; }
        NetEntityRole Role { get; }
        
        bool IsInitialized { get; }
        bool HasStateAuthority { get; }

        void Init(uint entityId, NetEntityRole role);
        void SetRole(NetEntityRole role);
    }
}
