namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 网络实体基础快照
    /// </summary>
    public struct NetEntitySnapshot : IEntitySnapshot
    {
        public uint EntityId { get; set; }
        public uint SnapshotTick { get; set; }
    }
}
