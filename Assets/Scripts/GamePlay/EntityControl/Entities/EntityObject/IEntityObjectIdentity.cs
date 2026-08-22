namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 实体对象身份接口。
    /// 由网络层或其他运行时身份提供者实现，实体核心不依赖具体实现。
    /// </summary>
    public interface IEntityObjectIdentity
    {
        uint EntityId { get; }
        bool IsInitialized { get; }
    }
}
