namespace GamePlay.EntitySystem
{
    /// <summary>
    /// 网络同步状态接收接口，用于应用权威或插值后的同步状态。
    /// </summary>
    public interface INetSyncSnapshotReceiver<TSnapshot> where TSnapshot : struct, IEntitySnapshot
    {
        /// <summary>
        /// 应用快照。
        /// </summary>
        void ApplySnapshot(in TSnapshot snapshot);
    }
}
