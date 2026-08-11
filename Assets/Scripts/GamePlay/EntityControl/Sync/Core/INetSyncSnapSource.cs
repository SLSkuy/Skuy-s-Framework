namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 网络同步状态来源接口，用于从实体或模块捕获同步状态。
    /// </summary>
    public interface INetSyncSnapSource<TSnapshot> where TSnapshot : struct, IEntitySnapshot
    {
        /// <summary>
        /// 捕获指定 Tick 的快照。
        /// </summary>
        TSnapshot CaptureSnapshot(uint snapshotTick, uint lastProcessedInputTick = 0);
    }
}

