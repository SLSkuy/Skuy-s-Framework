namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 按服务端 Tick 排序的快照契约。
    /// </summary>
    public interface IEntitySnapshot
    {
        uint SnapshotTick { get; set; }
    }
}
