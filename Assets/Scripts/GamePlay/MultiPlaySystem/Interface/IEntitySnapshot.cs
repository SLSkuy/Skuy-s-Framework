namespace GamePlay.EntitySystem
{
    public interface IEntitySnapshot
    {
        /// <summary>
        /// 快照所属的网络实体
        /// </summary>
        uint EntityId { get; set; }

        /// <summary>
        /// 生成该状态时的服务端模拟 Tick
        /// </summary>
        uint SnapshotTick { get; set; }
    }
}
